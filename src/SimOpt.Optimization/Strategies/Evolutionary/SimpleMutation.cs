using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.Optimization.Strategies.Evolutionary
{
    /// <summary>
    /// A class to mutate an <code>ITweaklable</code>, <code>IParametrizedTweakable&lt;int></code>
    /// or  <code>IParametrizedTweakable&lt;Tuple&lt;int, int>></code> ISolution using the 
    /// Tweak function. If the instance is IParametrizedTweakable with Tuple as type
    /// parameter, a tuple containing Size and Span (in this order) will be used as 
    /// argument for the Tweak function. If it is <code>IParametrizedTweakable&lt;int></code>
    /// only the Size will be used.
    /// </summary>
    public class SimpleMutation : IMutationOperator
    {
        #region prop

        #region IOperator

        /// <summary>
        /// Returns "Simple Mutation"
        /// </summary>
        public string Name
        {
            get { return "Simple Mutation"; }
        }

        /// <summary>
        /// Caution when changing the seed!
        /// </summary>
        public int Seed { get; set; }

        /// <summary>
        /// Returns 1.
        /// </summary>
        public int Cardinality
        {
            get { return 1; }
        }

        #endregion
        #region IMutationOperator

        /// <summary>
        /// Amount of mutation.
        /// </summary>
        public int Span { get; set; }

        /// <summary>
        /// Number of mutations to apply.
        /// </summary>
        public int Size { get; set; }

        #endregion

        #endregion
        #region impl

        #region IOperator

        /// <summary>
        /// Mutates an <code>ITweaklable</code>, <code>IParametrizedTweakable<int></code>
        /// or  <code>IParametrizedTweakable<Tuple<int, int>></code> ISolution using the 
        /// Tweak function. If the instance is IParametrizedTweakable with Tuple as type
        /// parameter, a tuple containing Size and Span (in this order) will be used as 
        /// argument for the Tweak function. If it is <code>IParametrizedTweakable<int></code>
        /// only the Size will be used.
        /// </summary>
        /// <param name="operands">
        /// Give exactly one operand which implements ITweaklable, 
        /// <code>IParametrizedTweakable<int></code> 
        /// or <code>IParametrizedTweakable<Tuple<int, int>></code>.
        /// </param>
        /// <returns>The modified operand.</returns>
        public ISolution Apply(params ISolution[] operands)
        {
            if (operands.Length != 1) 
                throw new ArgumentOutOfRangeException("This operator requires exactly one operand.");

            if (operands[0] is IParametrizedTweakable<Tuple<int, int>>)
            {
                (operands[0] as IParametrizedTweakable<Tuple<int, int>>).Tweak(new Tuple<int, int>(Size, Span));
                return operands[0];
            }
            else if (operands[0] is IParametrizedTweakable<int>)
            {
                (operands[0] as IParametrizedTweakable<int>).Tweak(Size);
                return operands[0];
            }
            else if (operands[0] is ITweakable)
            {
                (operands[0] as ITweakable).Tweak();
                return operands[0];
            }
            else
            {
                throw new ArgumentException("The operand has to be ITweakable or IParametrizedTweakable<Tuple<int, int>> for this operator.");
            }
        }

        #endregion
        #region IMutationOperator

        public void Initialize(int seed, int size, int span)
        {
            this.Seed = seed;
            this.Size = size;
            this.Span = span;
        }

        public void Configure(int size, int span)
        {
            this.Size = size;
            this.Span = span;
        }

        #endregion

        #endregion
    }
}