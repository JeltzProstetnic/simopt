using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Random = System.Random;
using MatthiasToolbox.Basics.Exceptions;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using System.Runtime.Serialization;
using System.Security.Permissions;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Mathematics.Stochastics.RandomSources
{
    /// <summary>
    /// pseudo random number generator as suggested in "A Very Fast Shift-Register Sequence Random Number Generator",
    /// J. Computational Physics, vol 40, pp. 517-526.
    /// with modification GFSR(521,168) to improve statistical properties
    /// </summary>
    /// <remarks>beta</remarks>
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

        public void Initialize(int seed, bool antithetic)
        {
            Antithetic = antithetic;
            this.seed = seed;
            System.Random rnd = new System.Random(seed);

            int i = 521;
            uint mask1 = 1;
            uint mask2 = 0xFFFFFFFF;

            while (i-- > 250)
            {
                r521_buffer[i] = (uint)rnd.Next();
            }

            while (i-- > 31)
            {
                r250_buffer[i] = (uint)rnd.Next();
                r521_buffer[i] = (uint)rnd.Next();
            }

            // enforce linear independence of the bit columns by setting the diagonal bits and clearing all bits above
            while (i-- > 0)
            {
                r250_buffer[i] = ((uint)rnd.Next() | mask1) & mask2;
                r521_buffer[i] = ((uint)rnd.Next() | mask1) & mask2;
                mask2 ^= mask1;
                mask1 >>= 1;
            }

            r250_buffer[0] = mask1;
            r521_buffer[0] = mask2;
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
            i2 = (i2 != 521) ? (i2 + 1) : 0;
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
            return antitheticSummandInteger + Math.Abs((int)NextUInt()) * antitheticFactor;
        }

        /// <summary>
        /// generates a random number on the interval [0, 1)
        /// caution: in RELEASE mode this will throw an IndexOutOfRangeException if the instance is not initialized! (a ClassInitializationException will be thrown in DEBUG mode)
        /// </summary>
        /// <returns></returns>
        public double NextDouble()
        {
            // TODO: check if this uint->double conversion is correct!
            return antitheticSummandDouble + ((double)NextUInt() / (double)uint.MaxValue) * antitheticFactor;
        }

        #region ISerializableGrubi

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
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
