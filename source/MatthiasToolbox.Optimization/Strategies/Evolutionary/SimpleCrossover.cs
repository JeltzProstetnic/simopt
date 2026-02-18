using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Optimization.Interfaces;

namespace MatthiasToolbox.Optimization.Strategies.Evolutionary
{
    /// <summary>
    /// A simple crossover operator. This only works for ISolutions 
    /// which implement <code>ICombinable<ISolution></code>.
    /// Caution: This operator does not support a growth factor 
    /// other than 2 and no predefined crossover size.
    /// </summary>
    public class SimpleCrossover : ICrossoverOperator
    {
        #region prop

        #region IOperator

        /// <summary>
        /// Returns "SimpleCrossover".
        /// </summary>
        public string Name
        {
            get { return "SimpleCrossover"; }
        }

        /// <summary>
        /// Be careful when changing the seed!
        /// </summary>
        public int Seed { get; set; }

        /// <summary>
        /// Always returns 2.
        /// </summary>
        public int Cardinality
        {
            get { return 2; }
        }

        #endregion
        #region ICrossoverOperator

        /// <summary>
        /// Not implemented!
        /// This operator does not support predefined crossover size.
        /// </summary>
        public int Size
        {
            get
            {
                throw new NotImplementedException("This crossover operator doesn't support predefined crossover size.");
            }
            set
            {
                throw new NotImplementedException("This crossover operator doesn't support predefined crossover size.");
            }
        }

        /// <summary>
        /// Only get is implemented and will always return 2.
        /// This operator does not support a growth factor other than 2.
        /// </summary>
        public int GrowthFactor
        {
            get { return 2; }
            set
            {
                throw new NotImplementedException("This crossover operator doesn't support changeing the growth factor.");
            }
        }

        #endregion

        #endregion
        #region impl

        #region IOperator

        /// <summary>
        /// Creates two children using the CombineWith function.
        /// The first child will be created combining the first
        /// operand with the second and the second child will be
        /// created combining the second with the first. Depending
        /// on the implementation of CombineWith this may result
        /// in twins, so make sure to not forget mutating the kids.
        /// </summary>
        /// <param name="operands">
        /// Give exactly two parameters which implement
        /// <code>ICombinable<ISolution></code>
        /// </param>
        /// <returns>Exactly two combined instances.</returns>
        public IEnumerable<ISolution> Apply(params ISolution[] operands)
        {
            if (operands.Length != 2) 
                throw new ArgumentOutOfRangeException("This operator only works with exactly two operands.");
            
            ICombinable<ISolution> op1 = operands[0] as ICombinable<ISolution>;
            ICombinable<ISolution> op2 = operands[1] as ICombinable<ISolution>;
            
            if (op1 == null || op2 == null) 
                throw new ArgumentException("The operands for this operator must not be null and must implement ICombinable<ISolution>.");
            
            yield return op1.CombineWith(operands[1]);
            yield return op2.CombineWith(operands[0]);
        }

        #endregion
        #region ICrossoverOperator

        /// <summary>
        /// Initialize this instance.
        /// Caution: This operator does not support a growth factor other than 2 and no predefined crossover size.
        /// </summary>
        /// <param name="seed">The random seed for this instance.</param>
        /// <param name="growthFactor">Not implemented!</param>
        /// <param name="size">Not implemented!</param>
        public void Initialize(int seed, int growthFactor = 2, int size = -1)
        {
            if (growthFactor != 2 || size != -1) 
                throw new ArgumentOutOfRangeException("This operator does not support a growth factor other than 2 and no predefined crossover size.");
            this.Seed = seed;
        }

        /// <summary>
        /// Not implemented!
        /// This operator does not support a growth factor other than 2 and no predefined crossover size.
        /// </summary>
        /// <param name="growFactor">Not implemented!</param>
        /// <param name="size">Not implemented!</param>
        public void Configure(int growFactor, int size)
        {
            throw new NotImplementedException("This operator does not support a growth factor other than 2 and no predefined crossover size.");
        }

        /// <summary>
        /// Not implemented! This operator does not support predefined crossover size.
        /// </summary>
        /// <param name="size">Not implemented!</param>
        public void Configure(int size)
        {
            throw new NotImplementedException("This operator does not support predefined crossover size.");
        }

        #endregion

        #endregion
    }
}
