using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Optimization.Interfaces;
using MatthiasToolbox.Mathematics.Stochastics;
using MatthiasToolbox.Optimization.Strategies.Evolutionary;

namespace MatthiasToolbox.SupplyChain.Optimizer
{
    public class Solution : ISolution, ITweakable, ICombinable<ISolution>
    {
        private int seed;
        private Random rnd;

        public List<int> TriggerLevels { get; set; }
        public List<int> InitialSupply { get; set; }
        public List<int> SupplyDelay { get; set; }

        public double Fitness { get; set; }

        public bool HasFitness { get; set; }

        public Solution(int seed)
        {
            this.seed = seed;
            rnd = new Random();
            TriggerLevels = new List<int>();
            InitialSupply = new List<int>();
            SupplyDelay = new List<int>();
        }

        public void Run()
        {
            Fitness = rnd.NextDouble() * 3000;
            HasFitness = true;
        }

        public int CompareTo(ISolution other)
        {
            if (other == null)
                throw new ArgumentNullException("other");

            if (!HasFitness || !other.HasFitness)
                throw new ApplicationException("Cannot compare solutions: not fitness value available.");

            return Fitness.CompareTo(other.Fitness);
        }

        public object Clone()
        {
            Solution clone = new Solution(seed);
            clone.Fitness = Fitness;
            clone.HasFitness = HasFitness;
            return clone;
        }

        #region ITweakable

        public void Tweak()
        {
            rnd.ExecuteConditionally(new Dictionary<Action, double> { { () => Fitness++, 0.5d }, { () => Fitness--, 0.5d } }); // Mathematics.Stochastics extensions
        }

        #endregion
        #region ICombinable<ISolution>

        public ISolution CombineWith(ISolution other)
        {
            return new Solution(seed);
        }

        #endregion
    }
}
