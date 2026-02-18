using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Optimization.Strategies.SimulatedAnnealing
{
    public class SimulatedAnnealingConfiguration : ISimulatedAnnealingConfiguration
    {
        #region cvar

        private double initialTemp = 100;
        private IBrownianOperator brownianOperator = new SimpleBrownian();

        #endregion
        #region prop

        #region ISimulatedAnnealingConfiguration

        public double InitialTemperature
        {
            get
            {
                return initialTemp;
            }
            set
            {
                initialTemp = value;
            }
        }

        public IBrownianOperator Brownian
        {
            get
            {
                return brownianOperator;
            }
            set
            {
                brownianOperator = value;
            }
        }

        public Func<AnnealingAlgorithm, double> DecreaseTemperature { get; set; }

        #endregion

        #region IConfiguration Member

        public int Seed { get; set; }

        public int NumberOfIterations { get; set; }
        
        public int NumberOfEvaluations { get; set; }
        
        #endregion

        #region ISolution

        public double Fitness { get; set; }
       
        public bool HasFitness { get; set; }
       
        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// Default constructor. The DecreaseTemperature function will be configured to decrease the temperature by 1 in each step.
        /// </summary>
        public SimulatedAnnealingConfiguration()
        {
            DecreaseTemperature = a => a.CurrentTemperature - 1;
        }

        #endregion
        #region impl

        #region IComparable<ISolution>

        public int CompareTo(Interfaces.ISolution other)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ICloneable

        public object Clone()
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion
    }
}