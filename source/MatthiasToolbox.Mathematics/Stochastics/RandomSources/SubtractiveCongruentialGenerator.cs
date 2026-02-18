using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Exceptions;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using System.Security.Permissions;
using System.Runtime.Serialization;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Mathematics.Stochastics.RandomSources
{
    /// <summary>
    /// a pseudo random number generator; more precisely the subtractive congruential 
    /// generator used in the System.Random namespace of .NET wrapped to implement IRandomGenerator
    /// </summary>
    /// <remarks>beta</remarks>
    [Serializable]
    public class SubtractiveCongruentialGenerator : IRandomSource, ISerializableSimulation
    {
        #region cvar

        private int seed = 0;
        private System.Random rnd;
        private bool initialized = false;
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
            get { return "C# Multiplicative Congruential Generator"; }
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
        /// create an instance of MultiplicativeCongruentialGenerator. caution: you have to initialize it before you can use it!
        /// </summary>
        public SubtractiveCongruentialGenerator() { }

        /// <summary>
        /// create an instance of MultiplicativeCongruentialGenerator and initializes it with the given seed. the instance can be used immediately.
        /// </summary>
        /// <param name="seed"></param>
        public SubtractiveCongruentialGenerator(int seed)
        {
            Initialize(seed);
        }

        public SubtractiveCongruentialGenerator(int seed, bool antithetic)
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
            rnd = new System.Random(seed);
            initialized = true;
        }

        public void Initialize(bool antithetic)
        {
            Initialize(Environment.TickCount, antithetic);
        }

        #endregion
        #region impl

        /// <summary>
        /// generates a random number on the interval [0, int.MaxValue)
        /// caution: this will throw a null reference exception if the instance is not initialized! (or a ClassInitializationException in debug mode)
        /// </summary>
        /// <returns></returns>
        public int NextInteger()
        {
#if DEBUG
            if (!initialized) throw new InitializationException("This instance of " + Name + " was not initialized!");
#endif
            return antitheticSummandInteger + rnd.Next() * antitheticFactor;
        }

        /// <summary>
        /// generates a random number on the interval [0, 1)
        /// caution: this will throw a null reference exception if the instance is not initialized! (or a ClassInitializationException in debug mode)
        /// </summary>
        /// <returns></returns>
        public double NextDouble()
        {
#if DEBUG
            if (!initialized) throw new InitializationException("This instance of " + Name + " was not initialized!");
#endif
            return antitheticSummandDouble + rnd.NextDouble() * antitheticFactor;
        }

        #region ISerializableGrubi

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("seed", seed);
            info.AddValue("rnd", rnd);
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

        /// <summary>
        /// reset this instance with a new seed
        /// </summary>
        /// <param name="seed"></param>
        public void Reset(int seed)
        {
            Reset(seed, antithetic);
        }

        /// <summary>
        /// reset this instance with a new seed changing the antithetic status
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="antithetic"></param>
        public void Reset(int seed, bool antithetic)
        {
            Antithetic = antithetic;
            rnd = new System.Random(seed);
        }

        /// <summary>
        /// reset this instance changing the antithetic status
        /// </summary>
        /// <param name="antithetic"></param>
        public void Reset(bool antithetic)
        {
            Reset(seed, antithetic);
        }

        #endregion
    }
}