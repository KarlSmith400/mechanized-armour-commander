using System;
using System.Collections.Generic;

namespace MechanizedArmourCommander.Core.Models
{
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        public int Q { get; }
        public int R { get; }
        public int S => -Q - R;

        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        // Distance between two hexes (cube distance)
        public static int Distance(HexCoord a, HexCoord b)
        {
            return (Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R) + Math.Abs(a.S - b.S)) / 2;
        }

        // Six pointy-top neighbor directions: E, NE, NW, W, SW, SE
        private static readonly HexCoord[] Directions = new HexCoord[]
        {
            new(+1,  0), new(+1, -1), new( 0, -1),
            new(-1,  0), new(-1, +1), new( 0, +1)
        };

        public HexCoord Neighbor(int direction)
        {
            var d = Directions[direction % 6];
            return new HexCoord(Q + d.Q, R + d.R);
        }

        public IEnumerable<HexCoord> AllNeighbors()
        {
            for (int i = 0; i < 6; i++)
                yield return Neighbor(i);
        }

        // All hexes within range steps of center
        public static List<HexCoord> HexesInRange(HexCoord center, int range)
        {
            var results = new List<HexCoord>();
            for (int q = -range; q <= range; q++)
            {
                int r1 = Math.Max(-range, -q - range);
                int r2 = Math.Min(range, -q + range);
                for (int r = r1; r <= r2; r++)
                    results.Add(new HexCoord(center.Q + q, center.R + r));
            }
            return results;
        }

        // Convert axial hex to pixel position (pointy-top orientation)
        public (double x, double y) ToPixel(double hexSize)
        {
            double x = hexSize * (Math.Sqrt(3.0) * Q + Math.Sqrt(3.0) / 2.0 * R);
            double y = hexSize * (3.0 / 2.0 * R);
            return (x, y);
        }

        // Convert pixel position back to axial hex (pointy-top, for mouse click hit-testing)
        public static HexCoord FromPixel(double px, double py, double hexSize)
        {
            double q = (Math.Sqrt(3.0) / 3.0 * px - 1.0 / 3.0 * py) / hexSize;
            double r = (2.0 / 3.0 * py) / hexSize;
            return CubeRound(q, r, -q - r);
        }

        private static HexCoord CubeRound(double fq, double fr, double fs)
        {
            int q = (int)Math.Round(fq);
            int r = (int)Math.Round(fr);
            int s = (int)Math.Round(fs);

            double dq = Math.Abs(q - fq);
            double dr = Math.Abs(r - fr);
            double ds = Math.Abs(s - fs);

            if (dq > dr && dq > ds)
                q = -r - s;
            else if (dr > ds)
                r = -q - s;

            return new HexCoord(q, r);
        }

        // Line draw between two hexes (for LOS / path display)
        public static List<HexCoord> LineDraw(HexCoord a, HexCoord b)
        {
            int dist = Distance(a, b);
            var results = new List<HexCoord>();
            if (dist == 0) { results.Add(a); return results; }

            for (int i = 0; i <= dist; i++)
            {
                double t = (double)i / dist;
                double fq = a.Q + (b.Q - a.Q) * t;
                double fr = a.R + (b.R - a.R) * t;
                double fs = a.S + (b.S - a.S) * t;
                results.Add(CubeRound(fq, fr, fs));
            }
            return results;
        }

        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;
        public override bool Equals(object? obj) => obj is HexCoord h && Equals(h);
        public override int GetHashCode() => HashCode.Combine(Q, R);
        public static bool operator ==(HexCoord a, HexCoord b) => a.Equals(b);
        public static bool operator !=(HexCoord a, HexCoord b) => !a.Equals(b);
        public override string ToString() => $"({Q},{R})";
    }
}
