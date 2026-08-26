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
    /// pseudo random number generator as suggested 1996 by Matsumora and Nishimura
    /// </summary>
    /// <remarks>beta</remarks>
    [Serializable]
    public class MersenneTwister : IRandomSource, ISerializableSimulation
    {
        #region cvar

        private int seed;
        private bool initialized;
        private int mt_index = -1;
        private uint[] mt_buffer = new uint[624];
        private bool antithetic = false;
        private int antitheticSummandInteger = 0;
        private int antitheticFactor = 1;

        #endregion
        #region prop

        public int Seed
        {
            get { return seed; }
        }

        public string Name
        {
            get { return "Mersenne Twister"; }
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
                    this.antitheticFactor = -1;
                }
                else
                {
                    this.antitheticSummandInteger = 0;
                    this.antitheticFactor = 1;
                }
            }
        }

        #endregion
        #region ctor

        /// <summary>
        /// create an instance of MersenneTwister. caution: you have to initialize it before you can use it!
        /// </summary>
        public MersenneTwister() { }

        /// <summary>
        /// create an instance of MersenneTwister and initializes it with the given seed. the instance can be used immediately.
        /// </summary>
        /// <param name="seed"></param>
        public MersenneTwister(int seed)
        {
            Initialize(seed);
        }

        public MersenneTwister(int seed, bool antithetic)
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

        /// <summary>
        /// Seeds the state with the reference MT19937 initialisation.
        /// </summary>
        /// <remarks>
        /// SIM-81. This previously filled the 624-word state from <c>System.Random.Next()</c> and
        /// set the index to 0, which had two consequences that together meant the class was not a
        /// Mersenne Twister at all for the first 624 draws. The twist runs only when the index
        /// reaches 624, so <b>those draws were System.Random's output verbatim</b>; and
        /// <c>Next()</c> returns <c>[0, int.MaxValue)</c>, so bit 31 was never set in any of them.
        /// <para>
        /// The reference recurrence below (Knuth's multiplier 1812433253, as used by
        /// <c>init_genrand</c>) fills the state from the seed alone, and the index starts at 624 so
        /// that the first draw twists — which is what makes the output match the published vector
        /// from draw one.
        /// </para>
        /// </remarks>
        public void Initialize(int seed, bool antithetic)
        {
            Antithetic = antithetic;
            this.seed = seed;

            mt_buffer[0] = (uint)seed;
            for (uint i = 1; i < 624; i++)
                mt_buffer[i] = 1812433253u * (mt_buffer[i - 1] ^ (mt_buffer[i - 1] >> 30)) + i;

            // 624, not 0: the reference generates a fresh block before returning anything. Starting
            // at 0 would hand out the raw seeded state, which is exactly what the old code did.
            mt_index = 624;
            initialized = true;
        }

        public void Initialize(bool antithetic)
        {
            Initialize(Environment.TickCount, antithetic);
        }

        #endregion
        #region impl

        /// <summary>
        /// Generates a raw 32-bit word, uniform over the whole range.
        /// CAUTION: this function IGNORES the antithetic flag!
        /// caution: in RELEASE mode this will throw an IndexOutOfRangeException if the instance is not initialized! (a ClassInitializationException will be thrown in DEBUG mode)
        /// </summary>
        /// <remarks>
        /// Public since SIM-81, because the only conclusive test of a Mersenne Twister is whether
        /// its raw words match the published reference vector, and that cannot be asserted through
        /// <see cref="NextInteger"/> — which masks bit 31 away, hiding precisely the defect SIM-81
        /// fixed.
        /// </remarks>
        public uint NextUInt()
        {
            // TODO: implement antithetic HERE instead of below?
#if DEBUG
            if (!initialized) throw new InitializationException("This instance of " + Name + " was not initialized!");
#endif
            if (mt_index == 624)
            {
                mt_index = 0;
                int i = 0;
                uint s;
                for (; i < 624 - 397; i++) {
                    s = (mt_buffer[i] & 0x80000000) | (mt_buffer[i+1] & 0x7FFFFFFF);
                    mt_buffer[i] = mt_buffer[i + 397] ^ (s >> 1) ^ ((s & 1) * 0x9908B0DF);
                }
                for (; i < 623; i++) {
                    s = (mt_buffer[i] & 0x80000000) | (mt_buffer[i+1] & 0x7FFFFFFF);
                    mt_buffer[i] = mt_buffer[i - (624 - 397)] ^ (s >> 1) ^ ((s & 1) * 0x9908B0DF);
                }
            
                s = (mt_buffer[623] & 0x80000000) | (mt_buffer[0] & 0x7FFFFFFF);
                mt_buffer[623] = mt_buffer[396] ^ (s >> 1) ^ ((s & 1) * 0x9908B0DF);
            }

            return Temper(mt_buffer[mt_index++]);
        }

        /// <summary>
        /// The MT19937 tempering transform, applied to a state word on its way out.
        /// </summary>
        /// <remarks>
        /// SIM-81: this step was missing entirely, and it is not cosmetic. The raw recurrence is
        /// only 1-dimensionally equidistributed to 32 bits; tempering is what gives MT19937 the
        /// 623-dimensional equidistribution it is chosen for, and without it the low bits carry
        /// visible linear structure. The state is not modified — tempering is a bijection applied
        /// to the output alone, so the recurrence above is untouched by it.
        /// </remarks>
        private static uint Temper(uint y)
        {
            y ^= y >> 11;
            y ^= (y << 7) & 0x9D2C5680u;
            y ^= (y << 15) & 0xEFC60000u;
            y ^= y >> 18;
            return y;
        }

        /// <summary>
        /// generates a random number on the interval [0, int.MaxValue)
        /// caution: in RELEASE mode this will throw an IndexOutOfRangeException if the instance is not initialized! (a ClassInitializationException will be thrown in DEBUG mode)
        /// </summary>
        /// <returns></returns>
        public int NextInteger()
        {
            // SIM-56: the previous Math.Abs((int)NextUInt()) threw OverflowException on the single
            // draw 0x80000000 and folded two draws onto int.MaxValue, putting NextDouble() on the
            // closed [0,1]. UniformMapping rejects the excluded endpoint instead.
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
            // Derived from NextInteger so the antithetic reflection applies to both.
            return UniformMapping.ToDouble(NextInteger());
        }

        #region ISerializableGrubi

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("seed", seed);
            info.AddValue("initialized", initialized);
            info.AddValue("mt_index", mt_index);
            info.AddValue("mt_buffer", mt_buffer);
            info.AddValue("antithetic", antithetic);
            info.AddValue("antitheticSummandInteger", antitheticSummandInteger);
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
