using System;

namespace SimOpt.Mathematics.Stochastics.RandomSources
{
    /// <summary>
    /// Maps a raw 32-bit generator draw onto the intervals that
    /// <see cref="Interfaces.IRandomSource"/> documents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every generator in this namespace produces raw 32-bit words and has to fold them onto
    /// <c>[0, int.MaxValue)</c> and <c>[0, 1)</c>. Three of them did it independently and two did it
    /// wrongly, so the mapping lives here once, as a pure function, and is tested at its boundaries
    /// rather than sampled and hoped over (SIM-56).
    /// </para>
    /// <para>
    /// The historical implementation was <c>Math.Abs((int)raw)</c>, which has two defects.
    /// It throws <see cref="OverflowException"/> for the single draw <c>0x80000000</c> —
    /// <c>Math.Abs(int.MinValue)</c> has no representable result — and it folds two distinct raw
    /// draws onto <c>int.MaxValue</c>, putting <c>NextDouble()</c> on the closed interval
    /// <c>[0, 1]</c> instead of the documented half-open one. A returned <c>1.0</c> breaks every
    /// inverse-transform sampler downstream.
    /// </para>
    /// </remarks>
    public static class UniformMapping
    {
        /// <summary>
        /// Scale factor folding a raw 32-bit word onto <c>[0, 1)</c>: 2^32, so the largest possible
        /// word maps just below one rather than onto it.
        /// </summary>
        private const double UIntScale = 4294967296.0;

        /// <summary>
        /// Folds a raw 32-bit draw onto <c>[0, int.MaxValue)</c>.
        /// </summary>
        /// <param name="raw">A raw generator word, uniform over the full 32-bit range.</param>
        /// <param name="value">The mapped value, valid only when this method returns true.</param>
        /// <returns>
        /// False when the draw folds onto the excluded endpoint and must be discarded. Rejecting
        /// keeps the mapping unbiased; clamping would over-weight the top of the range and
        /// wrapping would over-weight the bottom. The rejection probability is 2^-31, so the
        /// expected cost is well under one extra draw per two billion.
        /// </returns>
        public static bool TryMapToInteger(uint raw, out int value)
        {
            // Masking the sign bit clears it without discarding any other, so the cast is always
            // defined and always non-negative — unlike the Math.Abs form it replaces.
            //
            // Masking rather than shifting is deliberate. A one-bit right shift is equally
            // unbiased for a source that is uniform across all 32 bits, but MersenneTwister
            // pre-seeds its buffer from System.Random.Next(), which returns [0, int.MaxValue) and
            // therefore never sets the high bit. Under a shift its first 624 draws would be
            // confined to the lower half of the range and NextDouble() would return values in
            // [0, 0.5) — a far worse defect than the one being fixed. Masking keeps the low bits,
            // which carry the entropy in both the seeded and the twisted regime.
            value = (int)(raw & 0x7FFFFFFFu);
            return value != int.MaxValue;
        }

        /// <summary>
        /// Folds an integer draw on <c>[0, int.MaxValue)</c> onto <c>[0, 1)</c>.
        /// </summary>
        public static double ToDouble(int value) => value / (double)int.MaxValue;

        /// <summary>
        /// Folds a raw 32-bit draw onto <c>[0, 1)</c>, preserving the full 32 bits of resolution.
        /// </summary>
        public static double ToDouble(uint raw) => raw / UIntScale;
    }
}
