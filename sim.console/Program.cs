using System;
using System.Text;
using NeonCity.Sim;

namespace NeonCity.Sim.ConsoleApp
{
    /// <summary>
    /// Headless balance harness: lays out a starter town, runs N months and prints a
    /// monthly ledger plus an ASCII map. Usage: dotnet run -- [months] [seed]
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            int months = args.Length > 0 ? int.Parse(args[0]) : 36;
            ulong seed = args.Length > 1 ? ulong.Parse(args[1]) : 1;

            var cfg = new SimConfig();
            var city = new City(cfg, seed);
            BuildStarterTown(city);

            Console.WriteLine($"Seed {seed}, {cfg.Width}x{cfg.Height}, start money {cfg.StartingMoney}, after build {city.Money}");
            Console.WriteLine("Month |   Money |  Net | Pop  | Jobs | Power        | Demand");
            city.MonthEnded += r => Console.WriteLine(
                $"{r.Month,5} | {r.Money,7} | {r.Net,4} | {city.Population,4} | {city.Jobs,4} | {city.Power.Demand,4}/{city.Power.Capacity,-4} MW | {city.Demand}");
            city.WentBankrupt += () => Console.WriteLine("*** BANKRUPT ***");

            city.Step(months * cfg.TicksPerMonth);

            Console.WriteLine();
            Console.WriteLine(Render(city));
            Console.WriteLine("Legend: # road  P plant  r/c/i empty lot  R/C/I built (digit = density)  ! unpowered  ? no road");
            return city.IsBankrupt ? 1 : 0;
        }

        private static void BuildStarterTown(City city)
        {
            GridPos P(int x, int y) => new GridPos(x, y);
            void Do(CityCommand cmd)
            {
                var r = city.Execute(cmd);
                if (!r.Success) Console.WriteLine($"  build step failed: {cmd.GetType().Name} -> {r.Error}");
            }

            // Main street + three cross streets, plant at the west end.
            Do(new BuildRoadCommand(RoadPath.LShape(P(4, 20), P(44, 20))));
            foreach (int x in new[] { 14, 24, 34 })
                Do(new BuildRoadCommand(RoadPath.LShape(P(x, 12), P(x, 28))));
            Do(new PlacePowerPlantCommand(P(3, 20)));

            // Residential north, commercial along main street, industry south-east.
            Do(new ZoneCommand(GridRect.FromCorners(P(5, 22), P(23, 26)), ZoneType.Residential));
            Do(new ZoneCommand(GridRect.FromCorners(P(25, 21), P(33, 23)), ZoneType.Commercial));
            Do(new ZoneCommand(GridRect.FromCorners(P(25, 14), P(40, 18)), ZoneType.Industrial));
        }

        private static string Render(City city)
        {
            var sb = new StringBuilder();
            for (int y = 30; y >= 10; y--)
            {
                for (int x = 0; x < 46; x++)
                {
                    var c = city.GetCell(x, y);
                    sb.Append(c.Kind switch
                    {
                        TileKind.Road => '#',
                        TileKind.PowerPlant => 'P',
                        TileKind.Zoned when !c.Powered && c.IsBuilt => '!',
                        TileKind.Zoned when !c.RoadAccess => '?',
                        TileKind.Zoned when c.Density == 0 => char.ToLowerInvariant(c.Zone.ToString()[0]),
                        TileKind.Zoned => (char)('0' + c.Density),
                        _ => '.',
                    });
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
