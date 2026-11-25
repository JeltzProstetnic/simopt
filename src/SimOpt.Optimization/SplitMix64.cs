namespace SimOpt.Optimization
{
    /// <summary>
    /// SIM-59, SIM-68 D8: the generator a solution candidate carries for its moves (Steele, Lea and Flood
    /// 2014, "SplitMix64"). Its whole state is one 64-bit number held by the candidate, so a search started
    /// from the same seed takes the same path. A wall-clock <c>new Random()</c> in the operators broke that.
    /// </summary>
    /// <remarks>
    /// A value type: keep it in a field that is not readonly, or every draw works on a copy. A candidate's
    /// clone takes <see cref="Split"/>, not a copy: strategies tweak a clone of an unchanged point (SA after
    /// a rejected move, the EA mutating a surviving parent), and a copied state made every such clone
    /// propose the same move again, which froze a cold annealing run.
    /// </remarks>
    public struct SplitMix64
    {
        private ulong state;

        public SplitMix64(ulong state) => this.state = state;

        /// <summary>The next 64 random bits.</summary>
        public ulong Next()
        {
            ulong z = state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>A uniform draw on [0, 1).</summary>
        public double Uniform() => (Next() >> 11) * (1d / (1UL << 53));

        /// <summary>A new generator seeded from this one's next draw; this one moves on.</summary>
        public SplitMix64 Split() => new SplitMix64(Next());

        /// <summary>A whole number from 0 to <paramref name="count"/> - 1.</summary>
        public int Below(int count) => (int)(Next() % (ulong)count);
    }
}
