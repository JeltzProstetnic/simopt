using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Optimization.Interfaces;
using SimOpt.Mathematics.Stochastics;

namespace SimOpt.Optimization
{
    public static class Extensions
    {
        #region List

        /// <summary>
        /// Tournament selection. If t &lt;= 1 this will act as a pure random selection.
        /// If t >> source.Count this will return the fittest item with a high probability.
        /// </summary>
        /// <param name="source">The list to select from</param>
        /// <param name="rnd">A random number generator</param>
        /// <param name="t">Number of tournament rounds</param>
        /// <returns>A </returns>
        public static ISolution TournamentSelect(this List<ISolution> source, Random rnd, int t)
        {
            ISolution candidate = source.RandomItem(rnd);
            for (int i = 1; i < t; i++)
            {
                ISolution tmp = source.RandomItem(rnd);
                candidate = candidate.Fitness > tmp.Fitness ? candidate : tmp;
            }
            return candidate;
        }

        #endregion
    }
}
