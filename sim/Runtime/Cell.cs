namespace NeonCity.Sim
{
    public enum TileKind : byte
    {
        Empty = 0,
        Road = 1,
        PowerPlant = 2,
        Zoned = 3,
    }

    public enum ZoneType : byte
    {
        None = 0,
        Residential = 1,
        Commercial = 2,
        Industrial = 3,
    }

    /// <summary>
    /// One grid cell. A value type: <see cref="City.GetCell(int,int)"/> returns a copy,
    /// so the view can never mutate simulation state by accident.
    /// </summary>
    public struct Cell
    {
        public TileKind Kind { get; internal set; }

        /// <summary>Zone type when <see cref="Kind"/> is <see cref="TileKind.Zoned"/>, else None.</summary>
        public ZoneType Zone { get; internal set; }

        /// <summary>0 = empty lot, 1..MaxDensity = built and growing.</summary>
        public byte Density { get; internal set; }

        /// <summary>Connected to a power plant with spare capacity this tick.</summary>
        public bool Powered { get; internal set; }

        /// <summary>A road lies within <see cref="SimConfig.RoadAccessRadius"/>.</summary>
        public bool RoadAccess { get; internal set; }

        public bool IsBuilt => Kind == TileKind.Zoned && Density > 0;

        internal static Cell Empty => default;

        internal static Cell Road() => new Cell { Kind = TileKind.Road };

        internal static Cell Plant() => new Cell { Kind = TileKind.PowerPlant };

        internal static Cell Zoned(ZoneType zone) => new Cell { Kind = TileKind.Zoned, Zone = zone };

        public override string ToString() =>
            Kind == TileKind.Zoned ? $"{Zone}:{Density}{(Powered ? "" : " !pwr")}{(RoadAccess ? "" : " !road")}" : Kind.ToString();
    }
}
