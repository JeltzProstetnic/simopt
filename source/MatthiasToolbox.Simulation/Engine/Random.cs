using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Mathematics.Stochastics;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using System.Runtime.Serialization;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Simulation.Engine
{
    /// <summary>
    /// A wrapper for MatthiasToolbox.Mathematics.Stochastics.Random<T> to enable
    /// automatic centralized management of seed values.
    /// </summary>
    /// <typeparam name="T">the number type which will be returned by the distribution</typeparam>
    /// <remarks>beta</remarks>
    [Serializable]
    public class Random<T> : MatthiasToolbox.Mathematics.Stochastics.Random<T>, IResettable, IRandom, ISerializableSimulation
    {
        #region prop

        /// <summary>
        /// Random manager for discrete simulation engine.
        /// A seed will be set automatically by the seed source.
        /// If no seed has been set yet, this will return null.
        /// </summary>
        public int? Seed
        {
            get
            {
                return dist.Seed;
            }
            set
            {
                if (value == null) throw new ArgumentNullException("The seed cannot be set to null.");
                Reset((int)value); // the cast to int is actually not necessary, astoundingly the compiler detects the meaning of the above!
            }
        }

        /// <summary>
        /// Caution: setting this will reset the generator.
        /// This should be done through the model.
        /// </summary>
        public bool Antithetic
        {
            get
            {
                return dist.Antithetic;
            }
            set
            {
                Reset(true);
            }
        }

        #endregion
        #region ctor

        protected Random(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            // noop
        }

        /// <summary>
        /// Create a new random number generator based on the given distribution using
        /// a seed which will be automatically retrieved from the given seed source.
        /// </summary>
        /// <param name="seedSource"></param>
        /// <param name="distribution"></param>
        public Random(ISeedSource seedSource, IDistribution<T> distribution, bool antithetic = false, bool nonStochastic = false) : base(distribution, nonStochastic)
        {
        	if(distribution == null) throw new ArgumentNullException("distribution");
        	if(!distribution.Configured) throw new ArgumentException("You must provide a configured distribution.", "distribution");
        	if(distribution.Initialized) throw new ArgumentException("You must not provide an initialized distribution.", "distribution");
            
            distribution.Initialize(seedSource.SeedGenerator.Next(), antithetic);
            seedSource.AddRandomGenerator(this);
        }

        #endregion
        #region impl

        #region ISerializableGrubi

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        #endregion

        #endregion
        #region rset

        /// <summary>
        /// Reset this instance. Settings and seed
        /// will be preserved.
        /// </summary>
        public void Reset() 
        {
            Reset(Antithetic, NonStochasticMode);
        }

        /// <summary>
        /// Reset this instance using a new seed and
        /// changed settings.
        /// </summary>
        /// <param name="seed">A new seed</param>
        /// <param name="antithetic"></param>
        /// <param name="nonStochasticMode"></param>
        public override void Reset(int seed, bool antithetic = false, bool nonStochasticMode = false)
        {
            base.Reset(seed, antithetic, nonStochasticMode);
        }

        /// <summary>
        /// Reset this instance using changed settings.
        /// </summary>
        /// <param name="antithetic"></param>
        /// <param name="nonStochasticMode"></param>
        public override void Reset(bool antithetic = false, bool nonStochasticMode = false)
        {
            base.Reset(antithetic, nonStochasticMode);
        }

        #endregion
    }
}
