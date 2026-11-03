using System.Collections.Generic;
using Xunit;
using static NeonCity.Sim.Tests.TestCities;

namespace NeonCity.Sim.Tests
{
    public class EconomyTests
    {
        [Fact]
        public void Upkeep_is_charged_at_month_end_only()
        {
            var city = Empty();
            Ok(city.Execute(new BuildRoadCommand(RoadPath.LShape(P(0, 0), P(9, 0)))));
            Ok(city.Execute(new PlacePowerPlantCommand(P(0, 1))));
            long afterBuild = city.Money;
            var reports = new List<MonthReport>();
            city.MonthEnded += reports.Add;

            city.Step(city.Config.TicksPerMonth - 1);
            Assert.Equal(afterBuild, city.Money);
            Assert.Empty(reports);

            city.Step();
            long expected = 10 * city.Config.RoadUpkeepPerMonth + city.Config.PowerPlantUpkeepPerMonth;
            Assert.Single(reports);
            Assert.Equal(expected, reports[0].Expenses);
            Assert.Equal(0, reports[0].Income);
            Assert.Equal(afterBuild - expected, city.Money);
            Assert.Equal(1, city.Month);
        }

        [Fact]
        public void Taxes_come_from_residents_and_jobs()
        {
            var city = StandardTown(c =>
            {
                c.StartingMoney = 1_000_000;
                c.PowerPlantCapacity = 10_000;
            });
            var reports = new List<MonthReport>();
            city.MonthEnded += reports.Add;

            city.Step(city.Config.TicksPerMonth * 6);

            var last = reports[reports.Count - 1];
            long expected = (long)System.Math.Round(
                city.Population * city.Config.TaxPerResidentPerMonth + city.Jobs * city.Config.TaxPerJobPerMonth);
            Assert.Equal(expected, last.Income);
            Assert.True(last.Income > 0);
        }

        [Fact]
        public void City_goes_bankrupt_after_consecutive_months_in_debt()
        {
            var city = Empty(c =>
            {
                c.StartingMoney = 3000; // exactly one plant, then nothing left
                c.MonthsInDebtBeforeBankruptcy = 3;
            });
            Ok(city.Execute(new PlacePowerPlantCommand(P(0, 0))));
            bool fired = false;
            city.WentBankrupt += () => fired = true;

            city.Step(city.Config.TicksPerMonth * 2);
            Assert.False(city.IsBankrupt);
            Assert.Equal(2, city.MonthsInDebt);

            city.Step(city.Config.TicksPerMonth);
            Assert.True(city.IsBankrupt);
            Assert.True(fired);
        }

        [Fact]
        public void Bankrupt_city_rejects_commands_and_stops_ticking()
        {
            var city = Empty(c =>
            {
                c.StartingMoney = 3000;
                c.MonthsInDebtBeforeBankruptcy = 1;
            });
            Ok(city.Execute(new PlacePowerPlantCommand(P(0, 0))));
            city.Step(city.Config.TicksPerMonth);
            Assert.True(city.IsBankrupt);
            int tick = city.Tick;

            city.Step(10);

            Assert.Equal(tick, city.Tick);
            Assert.Equal(CommandError.Bankrupt, city.Execute(new BulldozeCommand(GridRect.Single(P(0, 0)))).Error);
        }
    }
}
