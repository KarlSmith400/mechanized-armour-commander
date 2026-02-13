using System;
using System.Collections.Generic;
using System.Linq;
using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Combat
{
    public static class HexPathfinding
    {
        // Dijkstra: all hexes reachable within maxRange movement, accounting for terrain costs
        public static HashSet<HexCoord> GetReachableHexes(HexGrid grid, HexCoord start, int maxRange)
        {
            var reachable = new HashSet<HexCoord>();
            var costSoFar = new Dictionary<HexCoord, int> { [start] = 0 };
            var frontier = new PriorityQueue<HexCoord, int>();
            frontier.Enqueue(start, 0);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                int currentCost = costSoFar[current];

                foreach (var neighbor in current.AllNeighbors())
                {
                    if (!grid.IsValid(neighbor)) continue;
                    if (grid.IsOccupied(neighbor)) continue;

                    var cell = grid.GetCell(neighbor);
                    int moveCost = cell != null ? HexGrid.GetTerrainMoveCost(cell.Terrain) : 1;
                    int newCost = currentCost + moveCost;
                    if (newCost > maxRange) continue;

                    if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                    {
                        costSoFar[neighbor] = newCost;
                        reachable.Add(neighbor);
                        frontier.Enqueue(neighbor, newCost);
                    }
                }
            }

            return reachable;
        }

        // A* pathfinding: shortest path from start to end within maxRange steps
        public static List<HexCoord> FindPath(HexGrid grid, HexCoord start, HexCoord end, int maxRange)
        {
            var cameFrom = new Dictionary<HexCoord, HexCoord>();
            var costSoFar = new Dictionary<HexCoord, int>();
            var frontier = new PriorityQueue<HexCoord, int>();

            frontier.Enqueue(start, 0);
            costSoFar[start] = 0;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (current == end) break;

                foreach (var next in current.AllNeighbors())
                {
                    if (!grid.IsValid(next)) continue;
                    if (next != end && grid.IsOccupied(next)) continue;

                    var nextCell = grid.GetCell(next);
                    int moveCost = nextCell != null ? HexGrid.GetTerrainMoveCost(nextCell.Terrain) : 1;
                    int newCost = costSoFar[current] + moveCost;
                    if (newCost > maxRange) continue;

                    if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                    {
                        costSoFar[next] = newCost;
                        int priority = newCost + HexCoord.Distance(next, end);
                        frontier.Enqueue(next, priority);
                        cameFrom[next] = current;
                    }
                }
            }

            if (!cameFrom.ContainsKey(end)) return new List<HexCoord>();

            var path = new List<HexCoord>();
            var step = end;
            while (step != start)
            {
                path.Add(step);
                step = cameFrom[step];
            }
            path.Reverse();
            return path;
        }

        // All hexes within weapon range (straight distance, ignoring obstacles)
        public static HashSet<HexCoord> GetHexesInWeaponRange(HexGrid grid, HexCoord origin, int maxRange)
        {
            var result = new HashSet<HexCoord>();
            var candidates = HexCoord.HexesInRange(origin, maxRange);
            foreach (var hex in candidates)
            {
                if (hex == origin) continue;
                if (grid.IsValid(hex))
                    result.Add(hex);
            }
            return result;
        }

        // Get hexes in weapon range that contain enemy frames
        public static HashSet<HexCoord> GetTargetableHexes(HexGrid grid, HexCoord origin, int maxRange, HashSet<int> enemyFrameIds)
        {
            var result = new HashSet<HexCoord>();
            var inRange = GetHexesInWeaponRange(grid, origin, maxRange);
            foreach (var hex in inRange)
            {
                var cell = grid.GetCell(hex);
                if (cell?.OccupantFrameId != null && enemyFrameIds.Contains(cell.OccupantFrameId.Value))
                    result.Add(hex);
            }
            return result;
        }
    }
}
