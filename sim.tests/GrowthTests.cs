using Xunit;
using static NeonCity.Sim.Tests.TestCities;

namespace NeonCity.Sim.Tests
{
    public class GrowthTests
    {
        [Fact]
        public void Empty_city_wants_residents_and_industry()
        {
            var city = Empty();
            Assert.True(city.Demand.Residential > 0);
            Assert.True(city.Demand.Industrial > 0);
            Assert.True(city.Demand.Commercial >= 0);
        }

        [Fact]
        public void Serviced_residential_zone_grows()
        {
            var city = StandardTown();
            city.Step(city.Config.TicksPerMonth * 3);

            Assert.True(SumDensity(city, ZoneType.Residential) > 0);
            Assert.True(city.Population > 0);
        }

        [Fact]
        public void Unpowered_zone_never_grows()
        {
            var city = Empty(c => c.StartingMoney = 1_000_000);
            Ok(city.Execute(new BuildRoadCommand(RoadPath.LShape(P(0, 5), P(20, 5)))));
            Ok(city.Execute(new ZoneCommand(Rect(0, 6, 20, 7), ZoneType.Residential)));

            city.Step(500);

            Assert.Equal(0, SumDensity(city, ZoneType.Residential));
        }

        [Fact]
        public void Zone_without_road_access_never_grows()
        {
            var city = Empty(c =>
            {
                c.StartingMoney = 1_000_000;
                c.RoadAccessRadius = 1;
            });
            // Plant touches the zone directly (so it is powered) but no road anywhere.
            Ok(city.Execute(new PlacePowerPlantCommand(P(0, 0))));
            Ok(city.Execute(new ZoneCommand(Rect(1, 0, 6, 1), ZoneType.Residential)));

            city.Step(500);

            Assert.True(city.GetCell(3, 0).Powered);
            Assert.False(city.GetCell(3, 0).RoadAccess);
            Assert.Equal(0, SumDensity(city, ZoneType.Residential));
        }

        [Fact]
        public void Built_zones_decay_after_losing_power()
        {
            var city = StandardTown(c => c.StartingMoney = 1_000_000);
            city.Step(city.Config.TicksPerMonth * 6);
            int grown = SumDensity(city, ZoneType.Residential);
            Assert.True(grown > 0);

            Ok(city.Execute(new BulldozeCommand(GridRect.Single(P(0, 4)))));
            city.Step(400);

            Assert.Equal(0, SumDensity(city, ZoneType.Residential));
        }

        [Fact]
        public void Mixed_town_grows_all_three_zone_types()
        {
            var city = StandardTown(c =>
            {
                c.StartingMoney = 1_000_000;
                c.PowerPlantCapacity = 10_000;
            });

            city.Step(city.Config.TicksPerMonth * 12);

            Assert.True(SumDensity(city, ZoneType.Residential) > 0);
            Assert.True(SumDensity(city, ZoneType.Commercial) > 0);
            Assert.True(SumDensity(city, ZoneType.Industrial) > 0);
        }

        [Fact]
        public void Density_never_exceeds_max()
        {
            var city = StandardTown(c =>
            {
                c.StartingMoney = 1_000_000;
                c.PowerPlantCapacity = 10_000;
                c.GrowthChance = 1.0;
                c.MaxDensity = 2;
            });

            city.Step(city.Config.TicksPerMonth * 24);

            for (int x = 1; x <= 30; x++)
                Assert.True(city.GetCell(x, 6).Density <= 2);
        }
    }
}
