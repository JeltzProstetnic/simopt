using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Mathematics.Stochastics.Interfaces;
using System.Runtime.Serialization;
using SimOpt.Basics.Interfaces;

namespace SimOpt.Mathematics.Stochastics
{
    [Serializable]
    public class Random<T> : ISerializableSimulation
    {
        #region cvar

        private bool nonStochasticMode = false;
        protected IDistribution<T> dist;

        #endregion
        #region prop

        public IDistribution<T> Distribution
        {
            get { return dist; }
        }

        public bool NonStochasticMode
        {
            get
            {
                return nonStochasticMode;
            }
            set
            {
                Reset(dist.Antithetic, value);
            }
        }

        #endregion
        #region ctor

        protected Random(SerializationInfo info, StreamingContext context)
        {
            nonStochasticMode = info.GetBoolean("nonStochasticMode");
            dist = (IDistribution<T>)info.GetValue("dist", typeof(IDistribution<T>));
        }

        public Random(IDistribution<T> distribution, bool nonStochasticMode = false)
        {
            this.dist = distribution;
            this.nonStochasticMode = nonStochasticMode;
        }

        #endregion
        #region impl

        /// <summary>
        /// Retrieve the next random number.
        /// Caution: if the distribution on which this instance is based
        /// was not initialized previously an exception may be thrown.
        /// If non stochastic mode is set to true, the distribution's 
        /// mean value will be returned instead of the next random number.
        /// </summary>
        /// <returns></returns>
        public T Next()
        {
            if (nonStochasticMode)
            {
                return dist.NonStochasticValue;
            }
            else
            {
                return dist.Next();
            }
        }

        #region ISerializableGrubi

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("nonStochasticMode", nonStochasticMode);
            info.AddValue("dist", dist);
        }

        #endregion

        #endregion
        #region rset

        public virtual void Reset(bool antithetic = false, bool nonStochasticMode = false)
        {
            dist.Reset(antithetic);
            this.nonStochasticMode = nonStochasticMode;
        }

        public virtual void Reset(int seed, bool antithetic = false, bool nonStochasticMode = false)
        {
            dist.Reset(seed, antithetic);
            this.nonStochasticMode = nonStochasticMode;
        }

        #endregion
    }
}