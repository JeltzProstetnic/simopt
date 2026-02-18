using System;
using System.Collections.Generic;
using MatthiasToolbox.Optimization.Interfaces;

namespace MatthiasToolbox.Optimization.Strategies.Evolutionary
{
    /// <summary>
    /// Signature for a function to select individuals from a list.
    /// </summary>
    /// <param name="source">The list from which to pick items</param>
    /// <returns>A number of selected individuals</returns>
    public delegate IEnumerable<ISolution> FractionSelector(List<ISolution> source);

    /// <summary>
    /// Signature for a function to select pairs of individuals from a list.
    /// </summary>
    /// <param name="source">The list from which to pick items.</param>
    /// <returns>A number of selected pairs.</returns>
    public delegate IEnumerable<Tuple<ISolution, ISolution>> MatingSelector(List<ISolution> source);

    /// <summary>
    /// Signature for a function to select individuals from the given lists into a new generation.
    /// </summary>
    /// <param name="children">The children created by the current parents</param>
    /// <param name="parents">The current parents</param>
    /// <param name="elite">The current elite individuals</param>
    /// <returns>A new generation</returns>
    public delegate IEnumerable<ISolution> GenerationSelector(List<ISolution> children, List<ISolution> parents, List<ISolution> elite);

    /// <summary>
    /// Signature for a function to select elite individuals from the list of 
    /// current elite individuals and the current generation.
    /// </summary>
    /// <param name="currentGeneration">The current generation</param>
    /// <param name="currentElite">The current elite</param>
    /// <returns>A number of elite individuals</returns>
    public delegate IEnumerable<ISolution> EliteSelector(List<ISolution> currentGeneration, List<ISolution> currentElite);
}