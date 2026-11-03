using System;

namespace NeonCity.Sim.Tests
{
    internal static class TestCities
    {
        public static SimConfig SmallConfig(Action<SimConfig>? tweak = null)
        {
            var cfg = new SimConfig { Width = 32, Height = 16 };
            tweak?.Invoke(cfg);
            return cfg;
        }

        public static City Empty(Action<SimConfig>? tweak = null, ulong seed = 42) =>
            new City(SmallConfig(tweak), seed);

        public static GridPos P(int x, int y) => new GridPos(x, y);

        public static GridRect Rect(int x0, int y0, int x1, int y1) => GridRect.FromCorners(P(x0, y0), P(x1, y1));

        /// <summary>
        /// Standard test town: a road along y=5 (x 0..30), a plant at (0,4) touching it,
        /// and R / C / I strips of 10x2 just above the road.
        /// </summary>
        public static City StandardTown(Action<SimConfig>? tweak = null, ulong seed = 42)
        {
            var city = Empty(tweak, seed);
            BuildStandardTown(city);
            return city;
        }

        public static void BuildStandardTown(City city)
        {
            Ok(city.Execute(new BuildRoadCommand(RoadPath.LShape(P(0, 5), P(30, 5)))));
            Ok(city.Execute(new PlacePowerPlantCommand(P(0, 4))));
            Ok(city.Execute(new ZoneCommand(Rect(1, 6, 10, 7), ZoneType.Residential)));
            Ok(city.Execute(new ZoneCommand(Rect(11, 6, 20, 7), ZoneType.Commercial)));
            Ok(city.Execute(new ZoneCommand(Rect(21, 6, 30, 7), ZoneType.Industrial)));
        }

        public static void Ok(CommandResult result)
        {
            if (!result.Success) throw new InvalidOperationException($"Expected success, got {result.Error}");
        }

        public static int SumDensity(City city, ZoneType zone)
        {
            int sum = 0;
            for (int y = 0; y < city.Height; y++)
            for (int x = 0; x < city.Width; x++)
            {
                var c = city.GetCell(x, y);
                if (c.Kind == TileKind.Zoned && c.Zone == zone) sum += c.Density;
            }

            return sum;
        }
    }
}
