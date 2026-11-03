using System;
using System.Collections.Generic;

namespace NeonCity.Sim
{
    /// <summary>A command and the tick it was executed at (before that tick was simulated).</summary>
    public readonly struct LoggedCommand
    {
        public readonly int Tick;
        public readonly CityCommand Command;

        public LoggedCommand(int tick, CityCommand command)
        {
            Tick = tick;
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }
    }

    /// <summary>
    /// Rebuilds a city from seed + command log. Because the sim is deterministic, this is
    /// the save format: store (config, seed, log, tick) and replay on load.
    /// Also the basis for undo (replay the log minus its tail).
    /// </summary>
    public static class CityReplay
    {
        public static City Rebuild(SimConfig config, ulong seed, IEnumerable<LoggedCommand> log, int targetTick)
        {
            var city = new City(config, seed);
            var entries = new List<LoggedCommand>(log);
            int next = 0;

            while (true)
            {
                while (next < entries.Count && entries[next].Tick == city.Tick)
                {
                    var result = city.Execute(entries[next].Command);
                    if (!result.Success)
                        throw new InvalidOperationException(
                            $"Replay diverged at tick {city.Tick}: command {next} failed with {result.Error}.");
                    next++;
                }

                if (next < entries.Count && entries[next].Tick < city.Tick)
                    throw new InvalidOperationException("Command log is not in tick order.");

                if (city.Tick >= targetTick || city.IsBankrupt) break;
                city.Step();
            }

            return city;
        }
    }
}
