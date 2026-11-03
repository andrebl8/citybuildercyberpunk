using System.Collections.Generic;
using Xunit;
using static NeonCity.Sim.Tests.TestCities;

namespace NeonCity.Sim.Tests
{
    public class CommandTests
    {
        [Fact]
        public void New_city_has_starting_money_and_empty_grid()
        {
            var city = Empty();
            Assert.Equal(city.Config.StartingMoney, city.Money);
            Assert.Equal(TileKind.Empty, city.GetCell(3, 3).Kind);
            Assert.Equal(0, city.Population);
            Assert.Equal(0, city.Tick);
        }

        [Fact]
        public void Zoning_charges_per_cell_and_skips_roads()
        {
            var city = Empty();
            Ok(city.Execute(new BuildRoadCommand(new[] { P(2, 2) })));
            long before = city.Money;

            var result = city.Execute(new ZoneCommand(Rect(0, 0, 3, 3), ZoneType.Residential));

            Assert.True(result.Success);
            Assert.Equal(15, result.CellsChanged); // 16 cells minus the road
            Assert.Equal(before - 15 * city.Config.CostZonePerCell, city.Money);
            Assert.Equal(TileKind.Road, city.GetCell(2, 2).Kind);
            Assert.Equal(ZoneType.Residential, city.GetCell(0, 0).Zone);
        }

        [Fact]
        public void Commands_are_all_or_nothing_when_funds_are_short()
        {
            var city = Empty(c => c.StartingMoney = 50); // 5 road cells' worth

            var result = city.Execute(new BuildRoadCommand(RoadPath.LShape(P(0, 0), P(9, 0))));

            Assert.Equal(CommandError.InsufficientFunds, result.Error);
            Assert.Equal(50, city.Money);
            Assert.Equal(TileKind.Empty, city.GetCell(0, 0).Kind);
            Assert.Empty(city.CommandLog);
        }

        [Fact]
        public void Road_through_a_power_plant_is_blocked()
        {
            var city = Empty();
            Ok(city.Execute(new PlacePowerPlantCommand(P(5, 0))));

            var result = city.Execute(new BuildRoadCommand(RoadPath.LShape(P(0, 0), P(9, 0))));

            Assert.Equal(CommandError.Blocked, result.Error);
            Assert.Equal(TileKind.Empty, city.GetCell(0, 0).Kind);
        }

        [Fact]
        public void Road_over_an_empty_zoned_lot_replaces_it()
        {
            var city = Empty();
            Ok(city.Execute(new ZoneCommand(Rect(0, 0, 4, 0), ZoneType.Commercial)));

            Ok(city.Execute(new BuildRoadCommand(new[] { P(2, 0) })));

            Assert.Equal(TileKind.Road, city.GetCell(2, 0).Kind);
            Assert.Equal(ZoneType.None, city.GetCell(2, 0).Zone);
        }

        [Fact]
        public void Out_of_bounds_is_rejected()
        {
            var city = Empty();
            Assert.Equal(CommandError.OutOfBounds, city.Execute(new PlacePowerPlantCommand(P(-1, 0))).Error);
            Assert.Equal(CommandError.OutOfBounds,
                city.Execute(new ZoneCommand(Rect(30, 0, 40, 2), ZoneType.Industrial)).Error);
        }

        [Fact]
        public void Repeating_a_command_with_no_effect_reports_nothing_to_do()
        {
            var city = Empty();
            Ok(city.Execute(new BuildRoadCommand(new[] { P(1, 1) })));
            Assert.Equal(CommandError.NothingToDo, city.Execute(new BuildRoadCommand(new[] { P(1, 1) })).Error);
        }

        [Fact]
        public void Bulldoze_clears_everything_and_charges_per_non_empty_cell()
        {
            var city = Empty();
            Ok(city.Execute(new BuildRoadCommand(new[] { P(0, 0), P(1, 0) })));
            Ok(city.Execute(new PlacePowerPlantCommand(P(3, 0))));
            long before = city.Money;

            var result = city.Execute(new BulldozeCommand(Rect(0, 0, 4, 0)));

            Assert.Equal(3, result.CellsChanged);
            Assert.Equal(before - 3 * city.Config.CostBulldozePerCell, city.Money);
            Assert.Equal(0, city.RoadCount);
            Assert.Equal(0, city.PowerPlantCount);
        }

        [Fact]
        public void Preview_does_not_change_the_city()
        {
            var city = Empty();
            ulong hash = city.ComputeStateHash();

            var preview = city.Preview(new ZoneCommand(Rect(0, 0, 4, 4), ZoneType.Residential));

            Assert.True(preview.Success);
            Assert.Equal(25 * city.Config.CostZonePerCell, preview.Cost);
            Assert.Equal(hash, city.ComputeStateHash());
            Assert.Empty(city.CommandLog);
        }

        [Fact]
        public void L_shaped_road_path_is_contiguous_without_duplicates()
        {
            var path = RoadPath.LShape(P(0, 0), P(3, 2));

            Assert.Equal(new[] { P(0, 0), P(1, 0), P(2, 0), P(3, 0), P(3, 1), P(3, 2) }, path);
        }

        [Fact]
        public void Cell_changed_fires_for_edited_cells()
        {
            var city = Empty();
            var changed = new HashSet<GridPos>();
            city.CellChanged += p => changed.Add(p);

            Ok(city.Execute(new ZoneCommand(Rect(0, 0, 1, 0), ZoneType.Industrial)));

            Assert.Contains(P(0, 0), changed);
            Assert.Contains(P(1, 0), changed);
        }
    }
}
