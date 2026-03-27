using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.Optimization.Strategies.SimulatedAnnealing
{
    /// <summary>
    /// A simple implementation for a brownian operator making use of the Tweak() function of an ITweakable
    /// </summary>
    public class SimpleBrownian : IBrownianOperator
    {
        #region IOperator<ISolution>

        public string Name
        {
            get { return "Simple Brownian Operator"; }
        }

        public int Seed { get; set; }

        public int Cardinality { get { return 1; } }

        public double CurrentTemperature { get; set; }

        public ISolution Apply(params ISolution[] operands)
        {
            if (operands.Length != 1)
                throw new ArgumentOutOfRangeException("This operator requires exactly one operand.");

            if (operands[0] is IParametrizedTweakable<double> tweakableDouble)
            {
                tweakableDouble.Tweak(CurrentTemperature);
                return operands[0];
            }
            else if (operands[0] is ITweakable tweakable)
            {
                tweakable.Tweak();
                return operands[0];
            }
            else
            {
                throw new ArgumentException("The operand has to be ITweakable or IParametrizedTweakable<Tuple<int, int>> for this operator.");
            }
        }

        #endregion
    }
}