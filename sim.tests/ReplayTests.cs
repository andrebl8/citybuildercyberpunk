using Xunit;
using static NeonCity.Sim.Tests.TestCities;

namespace NeonCity.Sim.Tests
{
    public class ReplayTests
    {
        private static City PlayScript(ulong seed)
        {
            var city = StandardTown(c => c.StartingMoney = 100_000, seed);
            city.Step(45);
            Ok(city.Execute(new ZoneCommand(Rect(1, 3, 10, 4), ZoneType.Residential)));
            city.Step(60);
            Ok(city.Execute(new BulldozeCommand(Rect(11, 6, 12, 7))));
            city.Step(75);
            return city;
        }

        [Fact]
        public void Same_seed_and_commands_give_identical_cities()
        {
            Assert.Equal(PlayScript(7).ComputeStateHash(), PlayScript(7).ComputeStateHash());
        }

        [Fact]
        public void Different_seeds_give_different_cities()
        {
            Assert.NotEqual(PlayScript(7).ComputeStateHash(), PlayScript(8).ComputeStateHash());
        }

        [Fact]
        public void Rebuilding_from_seed_and_log_reproduces_the_city()
        {
            var original = PlayScript(123);

            var rebuilt = CityReplay.Rebuild(original.Config, original.Seed, original.CommandLog, original.Tick);

            Assert.Equal(original.Tick, rebuilt.Tick);
            Assert.Equal(original.Money, rebuilt.Money);
            Assert.Equal(original.Population, rebuilt.Population);
            Assert.Equal(original.ComputeStateHash(), rebuilt.ComputeStateHash());
            Assert.Equal(original.CommandLog.Count, rebuilt.CommandLog.Count);
        }

        [Fact]
        public void Log_records_the_tick_each_command_ran_at()
        {
            var city = PlayScript(1);

            Assert.Equal(0, city.CommandLog[0].Tick);
            Assert.Equal(45, city.CommandLog[5].Tick);
            Assert.Equal(105, city.CommandLog[6].Tick);
        }
    }
}
