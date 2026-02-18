using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Random = System.Random;
using MatthiasToolbox.Basics.Exceptions;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using System.Security.Permissions;
using System.Runtime.Serialization;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Mathematics.Stochastics.RandomSources
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

        public void Initialize(int seed, bool antithetic)
        {
            Antithetic = antithetic;
            this.seed = seed;
            System.Random rnd = new System.Random(seed);
            for (int i = 0; i < 624; i++)
                mt_buffer[i] = (uint)rnd.Next();
            mt_index = 0;
            initialized = true;
        }

        public void Initialize(bool antithetic)
        {
            Initialize(Environment.TickCount, antithetic);
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
            return mt_buffer[mt_index++];
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
            return (double)NextInteger() / (double)int.MaxValue;
        }

        #region ISerializableGrubi

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
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
