using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Exceptions;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using System.Security.Permissions;
using System.Runtime.Serialization;

namespace MatthiasToolbox.Mathematics.Stochastics.RandomSources
{
    /// <summary>
    /// a pseudo random number generator; more precisely the linear congruential 
    /// generator used in the java.util namespace (reduced to integer seeds for compatibility)
    /// </summary>
    /// <remarks>beta</remarks>
    public class LinearCongruentialGenerator : IRandomSource //, ISerializableGrubi
    {
		#region cvar

        private int seed;
		private long internalSeed;
        private bool initialized;
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
            get { return "Java Linear Congruential Generator"; }
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
        /// create an instance of LinearCongruentialGenerator. caution: you have to initialize it before you can use it!
        /// </summary>
        public LinearCongruentialGenerator() { }

        /// <summary>
        /// create an instance of LinearCongruentialGenerator and initializes it with the given seed. the instance can be used immediately.
        /// </summary>
        /// <param name="seed"></param>
        public LinearCongruentialGenerator(int seed)
        {
            Initialize(seed);
		}

        public LinearCongruentialGenerator(int seed, bool antithetic)
        {
            Initialize(seed, antithetic);
        }
		
		#endregion
        #region init

        /// <summary>
        /// initializes this instance using Environment.TickCount as seed.
        /// caution: avoid this initializer if you need reproducible results.
        /// </summary>
        public void Initialize() { Initialize(Environment.TickCount); }

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
            SetSeed(seed);
            initialized = true;
        }

        public void Initialize(bool antithetic)
        {
            Initialize(Environment.TickCount, antithetic);
        }

        #endregion
        #region impl
		
        /// <summary>
        /// generates a random number on the interval [0, n) with the given bit size where n is the maximal possible number with n bits
        /// CAUTION: this function IGNORES the antithetic flag!
        /// caution: in RELEASE mode this will NOT throw an exception if the instance is not initialized! (a ClassInitializationException will be thrown in DEBUG mode, though)
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
		private int Next(int bits) 
        {
            // TODO: implement antithetic HERE
#if DEBUG
            if (!initialized) throw new InitializationException("This instance of " + Name + " was not initialized!");
#endif
			unchecked 
            {
				internalSeed = (internalSeed * 0x5DEECE66DL + 0xBL) & ((1L << 48) - 1);
				return (int)(internalSeed >> (48 - bits));
			}
		}
		
        /// <summary>
        /// generates a random number on the interval [0, int.MaxValue)
        /// caution: in RELEASE mode this will NOT throw an exception if the instance is not initialized! (a ClassInitializationException will be thrown in DEBUG mode, though)
        /// </summary>
        /// <returns></returns>
		public int NextInteger() 
        { 
            return antitheticSummandInteger + Next(int.MaxValue) * antitheticFactor; 
        }
		
        /// <summary>
        /// generates a random number on the interval [0, n)
        /// CAUTION: this function IGNORES the antithetic flag!
        /// caution: in RELEASE mode this will NOT throw an exception if the instance is not initialized! (a ClassInitializationException will be thrown in DEBUG mode, though)
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
		private int NextInteger(int n) 
        {
            // TODO: implement antithetic HERE
			unchecked{
				if (n<=0)
					throw new ArgumentOutOfRangeException("n must be positive");

				if ((n & -n) == n)  // i.e., n is a power of 2
					return (int)((n * (long)Next(31)) >> 31);

				int bits, val;
				do {
					bits = Next(31);
					val = bits % n;
				} while(bits - val + (n-1) < 0);
				return val;
			}
		}
		
        /// <summary>
        /// TODO: test NextLong, implement antithetic on this
        /// CAUTION: this function IGNORES the antithetic flag!
        /// generates a random number on the interval [0, long.MaxValue)
        /// caution: in RELEASE mode this will NOT throw an exception if the instance is not initialized! (a ClassInitializationException will be thrown in DEBUG mode, though)
        /// </summary>
        /// <returns></returns>
		private long NextLong() 
        {
			return ((long)Next(32) << 32) + Next(32);
		}

        /// <summary>
        /// generates a random boolean
        /// caution: in RELEASE mode this will NOT throw an exception if the instance is not initialized! (a ClassInitializationException will be thrown in DEBUG mode, though)
        /// </summary>
        /// <returns></returns>
		public bool NextBoolean() { return (Next(1) != 0) ^ antithetic; }

        /// <summary>
        /// TODO: test NextFloat, implement antithetic on this
        /// CAUTION: this function IGNORES the antithetic flag!
        /// generates a random number between [0, float.MaxValue)
        /// caution: in RELEASE mode this will NOT throw an exception if the instance is not initialized! (a ClassInitializationException will be thrown in DEBUG mode, though)
        /// </summary>
        /// <returns></returns>
		private float NextFloat() 
        {
			return Next(24) / ((float)(1 << 24));
		}
		
        /// <summary>
        /// generates a random number on the interval [0, 1)
        /// caution: in RELEASE mode this will NOT throw an exception if the instance is not initialized! (a ClassInitializationException will be thrown in DEBUG mode, though)
        /// </summary>
        /// <returns></returns>
		public double NextDouble() 
        {
			return antitheticSummandDouble + ((((long)Next(26) << 27) + Next(27)) / (double)(1L << 53)) * antitheticFactor;
		}

        #region ISerializableGrubi

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("seed", seed);
            info.AddValue("internalSeed", internalSeed);
            info.AddValue("initialized", initialized);
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
            Reset(seed, antithetic);
        }

        public void Reset(int seed)
        {
            Reset(seed, antithetic);
        }

        public void Reset(int seed, bool antithetic)
        {
            Antithetic = antithetic;
            SetSeed(seed);
        }

        public void Reset(bool antithetic)
        {
            Reset(seed, antithetic);
        }

        #endregion
        #region util

        private void SetSeed(long seed)
        {
            this.internalSeed = (seed ^ 0x5DEECE66DL) & ((1L << 48) - 1);
        }

        #endregion
    }
}
