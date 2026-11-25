using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Random = System.Random;
using SimOpt.Basics.Exceptions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using System.Runtime.Serialization;
using SimOpt.Basics.Interfaces;

namespace SimOpt.Mathematics.Stochastics.RandomSources
{
    /// <summary>
    /// pseudo random number generator as suggested in "A Very Fast Shift-Register Sequence Random Number Generator",
    /// J. Computational Physics, vol 40, pp. 517-526.
    /// with modification GFSR(521,168) to improve statistical properties
    /// </summary>
    /// <remarks>beta. SIM-98: no published reference vector exists for its seeding, so a run made with it
    /// cannot be checked by a third party; use <see cref="MersenneTwister"/> for a defensible run.</remarks>
    public class R250_521 : IRandomSource, ISerializableSimulation
    {
        #region cvar

        private int seed;
        private bool initialized;
        private int r250_index = -1;
        private int r521_index = -1;
        private uint[] r250_buffer = new uint[250];
        private uint[] r521_buffer = new uint[521];
        private bool antithetic = false;
        private int antitheticSummandInteger = 0;
        private double antitheticSummandDouble = 0;
        private int antitheticFactor = 1;

        #endregion
        #region prop

        public int Seed
        {
            get { return seed; }
        }

        public string Name
        {
            get { return "R250/521 Random Generator"; }
        }

        public bool Initialized
        {
            get { return initialized; }
        }

        public bool Antithetic
        {
            get { return antithetic; }
            private set
            {
                antithetic = value;
                if (antithetic)
                {
                    this.antitheticSummandInteger = int.MaxValue - 1;
                    this.antitheticSummandDouble = 0.9999999999999999d;
                    this.antitheticFactor = -1;
                }
                else
                {
                    this.antitheticSummandInteger = 0;
                    this.antitheticSummandDouble = 0;
                    this.antitheticFactor = 1;
                }
            }
        }

        #endregion
        #region ctor

        /// <summary>
        /// create an instance of R250_521. caution: you have to initialize it before you can use it!
        /// </summary>
        public R250_521() { }

        /// <summary>
        /// create an instance of R250_521 and initializes it with the given seed. the instance can be used immediately.
        /// </summary>
        /// <param name="seed"></param>
        public R250_521(int seed) 
        {
            Initialize(seed);
        }

        public R250_521(int seed, bool antithetic)
        {
            Initialize(seed, antithetic);
        }

        #endregion
        #region init

        /// <summary>
        /// initializes this instance using Environment.TickCount as seed.
        /// caution: avoid this initializer if you need reproducible results.
        /// </summary>
        public void Initialize() { Initialize(Environment.TickCount, false); }

        /// <summary>
        /// initializes this instance with the given seed value
        /// </summary>
        /// <param name="seed"></param>
        public void Initialize(int seed)
        {
            Initialize(seed, false);
        }

        /// <remarks>
        /// SIM-98: the words used to come from <c>System.Random.Next()</c>, which has 31 bits, so bit 31 was
        /// 0 in every word but one; the "linear independence" step shifted its mask right from 1, so it
        /// set no high bit; and two slots were never filled. XOR feedback cannot create a bit a column does
        /// not have, so the top bit stayed almost always 0: of the first 1,000 doubles 0.4 % reached 0.5,
        /// of the first million 16 %. Now every word takes 32 bits from <see cref="MersenneTwister"/>
        /// (pinned to MT19937 by SIM-81), and 32 words spread through each buffer are made triangular
        /// (their diagonal bit set, every bit above it cleared, as in Kirkpatrick and Stoll's and Maier's
        /// R250), so the 32 bit columns are linearly independent and none can be all zero. There is no
        /// published reference vector for this seeding: not for a defensible run, use the Mersenne Twister.
        /// </remarks>
        public void Initialize(int seed, bool antithetic)
        {
            Antithetic = antithetic;
            this.seed = seed;
            var words = new MersenneTwister(seed);
            for (int k = 0; k < r250_buffer.Length; k++) r250_buffer[k] = words.NextUInt();
            for (int k = 0; k < r521_buffer.Length; k++) r521_buffer[k] = words.NextUInt();

            uint diagonal = 0x80000000u, keep = 0xFFFFFFFFu;
            for (int bit = 0; bit < 32; bit++)
            {
                int k250 = 7 * bit + 3, k521 = 16 * bit + 3;   // up to 220 of 250 and 499 of 521
                r250_buffer[k250] = (r250_buffer[k250] & keep) | diagonal;
                r521_buffer[k521] = (r521_buffer[k521] & keep) | diagonal;
                keep >>= 1;
                diagonal >>= 1;
            }

            r250_index = 0;
            r521_index = 0;

            initialized = true;
        }

        public void Initialize(bool antithetic)
        {
            Initialize(Environment.TickCount, false);
        }

        #endregion
        #region impl

        /// <summary>
        /// generates a random number on the interval [0, uint.MaxValue)
        /// CAUTION: this function IGNORES the antithetic flag!
        /// caution: in RELEASE mode this will throw an IndexOutOfRangeException if the instance is not initialized! (a ClassInitializationException will be thrown in DEBUG mode)
        /// </summary>
        /// <returns></returns>
        private uint NextUInt() 
        {
            // TODO: implement antithetic HERE!
#if DEBUG
            if (!initialized) throw new InitializationException("This instance of " + Name + " was not initialized!");
#endif
            int i1 = r250_index;
            int i2 = r521_index;
        
            int j1 = i1 - (250-103);
            if (j1 < 0) j1 = i1 + 103;
            int j2 = i2 - (521-168);
            if (j2 < 0) j2 = i2 + 168;
        
            uint r = r250_buffer[j1] ^ r250_buffer[i1];
            r250_buffer[i1] = r;
            uint s = r521_buffer[j2] ^ r521_buffer[i2];
            r521_buffer[i2] = s;
        
            i1 = (i1 != 249) ? (i1 + 1) : 0;
            r250_index = i1;
            // SIM-56: r521_buffer holds 521 entries, so its last valid index is 520. Wrapping at
            // 521 let the index reach one past the end and the next draw read out of bounds.
            i2 = (i2 != 520) ? (i2 + 1) : 0;
            r521_index = i2;
            
            return r ^ s;
        }

        /// <summary>
        /// generates a random number on the interval [0, int.MaxValue)
        /// caution: in RELEASE mode this will throw an IndexOutOfRangeException if the instance is not initialized! (a ClassInitializationException will be thrown in DEBUG mode)
        /// </summary>
        /// <returns></returns>
        public int NextInteger()
        {
            // SIM-56: see UniformMapping — Math.Abs((int)raw) overflows on 0x80000000 and admits
            // int.MaxValue, which the documented half-open interval excludes.
            int value;
            while (!UniformMapping.TryMapToInteger(NextUInt(), out value)) { }
            return antitheticSummandInteger + value * antitheticFactor;
        }

        /// <summary>
        /// generates a random number on the interval [0, 1)
        /// caution: in RELEASE mode this will throw an IndexOutOfRangeException if the instance is not initialized! (a ClassInitializationException will be thrown in DEBUG mode)
        /// </summary>
        /// <returns></returns>
        public double NextDouble()
        {
            // SIM-56: dividing by uint.MaxValue put this on the closed [0,1]; the contract is
            // half-open, and a returned 1.0 breaks inverse-transform samplers. Scale by 2^32.
            return antitheticSummandDouble + UniformMapping.ToDouble(NextUInt()) * antitheticFactor;
        }

        #region ISerializableGrubi

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("seed", seed);
            info.AddValue("initialized", initialized);
            info.AddValue("r250_index", r250_index);
            info.AddValue("r521_index", r521_index);
            info.AddValue("r250_buffer", r250_buffer);
            info.AddValue("r521_buffer", r521_buffer);
            info.AddValue("antithetic", antithetic);
            info.AddValue("antitheticSummandInteger", antitheticSummandInteger);
            info.AddValue("antitheticSummandDouble", antitheticSummandDouble);
            info.AddValue("antitheticFactor", antitheticFactor);
        }

        #endregion

        #endregion
        #region rset

        /// <summary>
        /// reset this instance
        /// </summary>
        public void Reset()
        {
            Initialize(seed, antithetic);
        }

        public void Reset(int seed)
        {
            Initialize(seed, antithetic);
        }

        public void Reset(int seed, bool antithetic)
        {
            Initialize(seed, antithetic);
        }

        public void Reset(bool antithetic)
        {
            Initialize(seed, antithetic);
        }

        #endregion
    }
}
