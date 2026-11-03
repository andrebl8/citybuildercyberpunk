namespace NeonCity.Sim
{
    /// <summary>
    /// Tiny deterministic PRNG. Same seed gives the same sequence on Mono, IL2CPP,
    /// WebGL and .NET, which System.Random does not promise. Needed for replay.
    /// </summary>
    internal sealed class SplitMix64
    {
        private ulong _state;

        public SplitMix64(ulong seed) => _state = seed;

        public ulong State => _state;

        public ulong NextULong()
        {
            unchecked
            {
                ulong z = _state += 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>Uniform in [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

        /// <summary>Uniform in [0, maxExclusive).</summary>
        public int NextInt(int maxExclusive) => (int)(NextULong() % (ulong)maxExclusive);
    }
}
