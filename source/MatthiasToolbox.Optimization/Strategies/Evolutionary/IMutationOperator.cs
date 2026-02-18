using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Optimization.Interfaces;

namespace MatthiasToolbox.Optimization.Strategies.Evolutionary
{
    /// <summary>
    /// Interface for a mutation operator class.
    /// Inherits <code>IOperator&lt;ISolution></code>
    /// </summary>
    public interface IMutationOperator : IOperator<ISolution>
    {
        /// <summary>
        /// The radius of mutation.
        /// </summary>
        int Span { get; set; }

        /// <summary>
        /// The number of events.
        /// </summary>
        int Size { get; set; }

        /// <summary>
        /// Set the initial configuration of this instance.
        /// </summary>
        /// <param name="seed">The random seed for stochastic operations.</param>
        /// <param name="size">The number of events.</param>
        /// <param name="span">The radius of mutation.</param>
        void Initialize(int seed, int size, int span);

        /// <summary>
        /// Set or change the configuration of this instance.
        /// </summary>
        /// <param name="size">The number of events.</param>
        /// <param name="span">The radius of mutation.</param>
        void Configure(int size, int span);
    }
}
