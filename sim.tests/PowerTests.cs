using Xunit;
using static NeonCity.Sim.Tests.TestCities;

namespace NeonCity.Sim.Tests
{
    public class PowerTests
    {
        [Fact]
        public void Power_flows_from_plant_through_roads_into_zones()
        {
            var city = StandardTown();

            Assert.True(city.GetCell(15, 5).Powered);  // road
            Assert.True(city.GetCell(5, 7).Powered);   // residential, second row
            Assert.True(city.GetCell(30, 6).Powered);  // far industrial
            Assert.Equal(0, city.Power.UnpoweredZones);
        }

        [Fact]
        public void Zone_not_connected_to_the_network_is_unpowered()
        {
            var city = StandardTown();
            Ok(city.Execute(new ZoneCommand(Rect(5, 12, 6, 12), ZoneType.Residential)));

            Assert.False(city.GetCell(5, 12).Powered);
            Assert.Equal(2, city.Power.UnpoweredZones);
        }

        [Fact]
        public void Over_capacity_network_browns_out_cells_farthest_from_the_plant()
        {
            // 60 empty lots use 1 each; capacity 30 serves only the nearest half.
            var city = StandardTown(c => c.PowerPlantCapacity = 30);

            Assert.True(city.GetCell(1, 6).Powered);
            Assert.False(city.GetCell(30, 7).Powered);
            Assert.Equal(30, city.Power.Capacity);
            Assert.Equal(60, city.Power.Demand);
            Assert.Equal(30, city.Power.UnpoweredZones);
        }

        [Fact]
        public void Bulldozing_the_plant_cuts_power()
        {
            var city = StandardTown();
            Ok(city.Execute(new BulldozeCommand(GridRect.Single(P(0, 4)))));

            Assert.False(city.GetCell(5, 6).Powered);
            Assert.Equal(60, city.Power.UnpoweredZones);
            Assert.Equal(0, city.Power.Capacity);
        }

        [Fact]
        public void Two_plants_on_one_network_share_capacity()
        {
            var city = StandardTown(c => c.PowerPlantCapacity = 30);
            Ok(city.Execute(new PlacePowerPlantCommand(P(30, 4))));

            Assert.Equal(60, city.Power.Capacity);
            Assert.Equal(0, city.Power.UnpoweredZones);
        }
    }
}
