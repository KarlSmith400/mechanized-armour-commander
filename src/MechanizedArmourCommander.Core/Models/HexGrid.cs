using System;
using System.Collections.Generic;
using System.Linq;

namespace MechanizedArmourCommander.Core.Models
{
    public enum HexTerrain
    {
        Open,
        Forest,
        Rocks,
        Rough,
        Sand
    }

    public class HexCell
    {
        public HexCoord Coord { get; set; }
        public HexTerrain Terrain { get; set; } = HexTerrain.Open;
        public int? OccupantFrameId { get; set; }
    }

    public class HexGrid
    {
        public int Width { get; }
        public int Height { get; }
        public string Landscape { get; }
        private readonly Dictionary<HexCoord, HexCell> _cells = new();

        public HexGrid(int width, int height, string landscape = "Habitable")
        {
            Width = width;
            Height = height;
            Landscape = landscape;
            Generate();
        }

        // Convert axial coord back to offset column (pointy-top, odd-r offset)
        public int OffsetCol(HexCoord coord) => coord.Q + (coord.R - (coord.R & 1)) / 2;

        private void Generate()
        {
            var rng = new Random();

            // Create rectangular grid using odd-r offset → axial conversion
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    int q = col - (row - (row & 1)) / 2;
                    int r = row;
                    var coord = new HexCoord(q, r);
                    _cells[coord] = new HexCell { Coord = coord };
                }
            }

            // Deployment zones stay Open (first 2 and last 2 columns by offset col)
            var interiorCells = _cells.Values
                .Where(c => OffsetCol(c.Coord) >= 2 && OffsetCol(c.Coord) < Width - 2)
                .ToList();

            // Terrain profile based on landscape (planet type)
            var (forestPct, rocksPct, roughPct, sandPct) = GetTerrainProfile(Landscape);

            // Scatter forest clusters
            if (forestPct > 0)
            {
                int forestTarget = (int)(interiorCells.Count * forestPct);
                int forestPlaced = 0;
                int attempts = 0;
                while (forestPlaced < forestTarget && attempts < 200)
                {
                    attempts++;
                    var seed = interiorCells[rng.Next(interiorCells.Count)];
                    if (seed.Terrain != HexTerrain.Open) continue;

                    seed.Terrain = HexTerrain.Forest;
                    forestPlaced++;

                    int clusterSize = rng.Next(1, 4);
                    foreach (var neighbor in seed.Coord.AllNeighbors())
                    {
                        if (clusterSize <= 0) break;
                        var cell = GetCell(neighbor);
                        if (cell != null && cell.Terrain == HexTerrain.Open &&
                            OffsetCol(cell.Coord) >= 2 && OffsetCol(cell.Coord) < Width - 2)
                        {
                            cell.Terrain = HexTerrain.Forest;
                            forestPlaced++;
                            clusterSize--;
                        }
                    }
                }
            }

            // Scatter rocks
            if (rocksPct > 0)
            {
                int rocksTarget = (int)(interiorCells.Count * rocksPct);
                for (int i = 0; i < rocksTarget; i++)
                {
                    var cell = interiorCells[rng.Next(interiorCells.Count)];
                    if (cell.Terrain == HexTerrain.Open)
                        cell.Terrain = HexTerrain.Rocks;
                }
            }

            // Scatter rough terrain
            if (roughPct > 0)
            {
                int roughTarget = (int)(interiorCells.Count * roughPct);
                for (int i = 0; i < roughTarget; i++)
                {
                    var cell = interiorCells[rng.Next(interiorCells.Count)];
                    if (cell.Terrain == HexTerrain.Open)
                        cell.Terrain = HexTerrain.Rough;
                }
            }

            // Scatter sand
            if (sandPct > 0)
            {
                int sandTarget = (int)(interiorCells.Count * sandPct);
                for (int i = 0; i < sandTarget; i++)
                {
                    var cell = interiorCells[rng.Next(interiorCells.Count)];
                    if (cell.Terrain == HexTerrain.Open)
                        cell.Terrain = HexTerrain.Sand;
                }
            }
        }

        /// <summary>
        /// Returns terrain generation percentages based on planet/landscape type.
        /// (forest%, rocks%, rough%, sand%)
        /// </summary>
        private static (double forest, double rocks, double rough, double sand) GetTerrainProfile(string landscape)
        {
            return landscape switch
            {
                // Green world — forests, rocks, some rough
                "Habitable" => (0.12, 0.10, 0.08, 0.0),
                // Factory floors and warehouses — lots of rough, rocks (structures), no vegetation
                "Industrial" => (0.0, 0.15, 0.20, 0.05),
                // Barren rock — heavy rocks, sand, rough terrain, no vegetation
                "Mining" => (0.0, 0.20, 0.10, 0.15),
                // Metal corridors and cargo bays — scattered rough (debris), some rocks (bulkheads)
                "Station" => (0.0, 0.12, 0.18, 0.0),
                // Sparse frontier — sand, rough, scattered rocks, no trees
                "Outpost" => (0.0, 0.10, 0.12, 0.15),
                _ => (0.12, 0.10, 0.08, 0.0)
            };
        }

        // Terrain gameplay properties
        public static int GetTerrainMoveCost(HexTerrain terrain) => terrain switch
        {
            HexTerrain.Forest => 2,
            HexTerrain.Rocks => 2,
            HexTerrain.Rough => 2,
            _ => 1
        };

        public static int GetTerrainDefenseBonus(HexTerrain terrain) => terrain switch
        {
            HexTerrain.Forest => 15,
            HexTerrain.Rocks => 10,
            _ => 0
        };

        /// <summary>
        /// Accuracy penalty for shooting through intervening terrain (excludes attacker and target hexes).
        /// Forest: -5 per hex, Rocks: -3 per hex.
        /// </summary>
        public static int GetInterveningTerrainPenalty(HexTerrain terrain) => terrain switch
        {
            HexTerrain.Forest => 5,
            HexTerrain.Rocks => 3,
            _ => 0
        };

        /// <summary>
        /// Calculate total LOS penalty from intervening terrain between two hexes.
        /// Walks the hex line, excluding the start and end hexes.
        /// Returns total penalty and the list of penalizing hexes for visualization.
        /// </summary>
        public (int totalPenalty, List<(HexCoord coord, int penalty)> interveningHexes) GetLOSPenalty(HexCoord from, HexCoord to)
        {
            var line = HexCoord.LineDraw(from, to);
            int total = 0;
            var intervening = new List<(HexCoord coord, int penalty)>();

            // Skip first (attacker) and last (target) hex
            for (int i = 1; i < line.Count - 1; i++)
            {
                var cell = GetCell(line[i]);
                if (cell == null) continue;
                int penalty = GetInterveningTerrainPenalty(cell.Terrain);
                if (penalty > 0)
                {
                    total += penalty;
                    intervening.Add((line[i], penalty));
                }
            }

            return (total, intervening);
        }

        public static string GetTerrainName(HexTerrain terrain) => terrain switch
        {
            HexTerrain.Open => "Open",
            HexTerrain.Forest => "Forest",
            HexTerrain.Rocks => "Rocks",
            HexTerrain.Rough => "Rough",
            HexTerrain.Sand => "Sand",
            _ => "Unknown"
        };

        public HexCell? GetCell(HexCoord coord) =>
            _cells.TryGetValue(coord, out var cell) ? cell : null;

        public bool IsValid(HexCoord coord) => _cells.ContainsKey(coord);

        public bool IsOccupied(HexCoord coord)
        {
            var cell = GetCell(coord);
            return cell?.OccupantFrameId != null;
        }

        public void PlaceFrame(int frameId, HexCoord coord)
        {
            var cell = GetCell(coord);
            if (cell != null) cell.OccupantFrameId = frameId;
        }

        public void RemoveFrame(HexCoord coord)
        {
            var cell = GetCell(coord);
            if (cell != null) cell.OccupantFrameId = null;
        }

        public void MoveFrame(int frameId, HexCoord from, HexCoord to)
        {
            RemoveFrame(from);
            PlaceFrame(frameId, to);
        }

        public IEnumerable<HexCell> AllCells => _cells.Values;

        public HexCoord? FindFrame(int frameId) =>
            _cells.Values.FirstOrDefault(c => c.OccupantFrameId == frameId)?.Coord;

        public List<HexCoord> GetDeploymentZone(bool isPlayer, int unitCount)
        {
            int startCol = isPlayer ? 0 : Width - 2;
            int endCol = isPlayer ? 1 : Width - 1;

            var zone = AllCells
                .Where(c => OffsetCol(c.Coord) >= startCol && OffsetCol(c.Coord) <= endCol && !IsOccupied(c.Coord))
                .Select(c => c.Coord)
                .OrderBy(c => OffsetCol(c)).ThenBy(c => c.R)
                .ToList();

            var positions = new List<HexCoord>();
            if (zone.Count == 0) return positions;

            int step = Math.Max(1, zone.Count / (unitCount + 1));
            for (int i = 0; i < unitCount && (i + 1) * step < zone.Count; i++)
                positions.Add(zone[(i + 1) * step]);

            // If we didn't get enough, fill from remaining
            while (positions.Count < unitCount && positions.Count < zone.Count)
            {
                var next = zone.FirstOrDefault(z => !positions.Contains(z));
                if (next == default && positions.Count > 0) break;
                positions.Add(next);
            }

            return positions;
        }

        /// <summary>
        /// Returns all hexes in the deployment zone (columns 0-1 or last 2 columns)
        /// </summary>
        public HashSet<HexCoord> GetFullDeploymentZone(bool isPlayer)
        {
            int startCol = isPlayer ? 0 : Width - 2;
            int endCol = isPlayer ? 1 : Width - 1;

            return AllCells
                .Where(c => OffsetCol(c.Coord) >= startCol && OffsetCol(c.Coord) <= endCol)
                .Select(c => c.Coord)
                .ToHashSet();
        }

        public static (int width, int height) GetDimensions(MapSize size) => size switch
        {
            MapSize.Small => (12, 10),
            MapSize.Medium => (16, 12),
            MapSize.Large => (20, 14),
            _ => (16, 12)
        };
    }
}
