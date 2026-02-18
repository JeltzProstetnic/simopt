using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Optimization.Interfaces
{
    /// <summary>
    /// Interface for an operator class.
    /// </summary>
    /// <typeparam name="T">The return type of the <code>Apply</code> function</typeparam>
    public interface IOperator<T>
    {
        /// <summary>
        /// Name of the operation.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// A seed for use in stochastic operations.
        /// </summary>
        int Seed { get; set; }
        
        /// <summary>
        /// Number of expected parameters. Set this to 
        /// <code>int.MaxValue</code> in case of an 
        /// arbitraty number of parameters.
        /// </summary>
        int Cardinality { get; }

        /// <summary>
        /// Perform the operation.
        /// </summary>
        /// <param name="operands">The input values.</param>
        /// <returns>Depends on the implementation.</returns>
        T Apply(params ISolution[] operands);
    }
}
