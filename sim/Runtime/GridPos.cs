using System;

namespace NeonCity.Sim
{
    /// <summary>A cell coordinate on the city grid. (0,0) is the bottom-left corner.</summary>
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public readonly int X;
        public readonly int Y;

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridPos other) => X == other.X && Y == other.Y;
        public override bool Equals(object? obj) => obj is GridPos other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public static bool operator ==(GridPos a, GridPos b) => a.Equals(b);
        public static bool operator !=(GridPos a, GridPos b) => !a.Equals(b);
        public override string ToString() => $"({X},{Y})";
    }

    /// <summary>An axis-aligned rectangle of cells, inclusive of both corners.</summary>
    public readonly struct GridRect
    {
        public readonly int MinX;
        public readonly int MinY;
        public readonly int MaxX;
        public readonly int MaxY;

        private GridRect(int minX, int minY, int maxX, int maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        /// <summary>Rectangle spanned by two drag corners, in any order.</summary>
        public static GridRect FromCorners(GridPos a, GridPos b) =>
            new GridRect(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

        public static GridRect Single(GridPos p) => new GridRect(p.X, p.Y, p.X, p.Y);

        public int Width => MaxX - MinX + 1;
        public int Height => MaxY - MinY + 1;
        public int Area => Width * Height;

        public override string ToString() => $"[{MinX},{MinY}..{MaxX},{MaxY}]";
    }
}
