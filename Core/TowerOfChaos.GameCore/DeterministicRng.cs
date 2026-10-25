namespace TowerOfChaos.GameCore
{
    /// <summary>
    /// SplitMix64. Own implementation so results are identical on every platform / runtime
    /// (System.Random is not guaranteed stable). Server and clients derive the same world from matchSeed (SPEC 14.1).
    /// </summary>
    public sealed class DeterministicRng
    {
        private ulong _s;

        public DeterministicRng(ulong seed) { _s = seed; }

        public static ulong Mix(ulong z)
        {
            unchecked
            {
                z += 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>Independent stream per (matchSeed, floor, room, stream).</summary>
        public static DeterministicRng For(long matchSeed, int floor, int room, int stream)
        {
            unchecked
            {
                ulong x = Mix((ulong)matchSeed);
                x = Mix(x ^ ((ulong)floor << 40));
                x = Mix(x ^ ((ulong)room << 20));
                x = Mix(x ^ (ulong)stream);
                return new DeterministicRng(x);
            }
        }

        public ulong NextU64()
        {
            unchecked
            {
                _s += 0x9E3779B97F4A7C15UL;
                ulong z = _s;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>Uniform in [0, maxExclusive).</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 1) return 0;
            return (int)(NextU64() % (ulong)maxExclusive);
        }

        /// <summary>Uniform in [min, maxExclusive).</summary>
        public int Next(int min, int maxExclusive) { return min + Next(maxExclusive - min); }

        public double NextDouble() { return (NextU64() >> 11) * (1.0 / 9007199254740992.0); }
    }
}
