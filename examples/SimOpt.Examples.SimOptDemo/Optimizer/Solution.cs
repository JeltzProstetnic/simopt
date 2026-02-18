using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Optimization.Interfaces;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Optimization.Strategies.Evolutionary;

namespace MatthiasToolbox.SimOptExample.Optimizer
{
    public class Solution : ISolution, IComparable<ISolution>, ICloneable, ICombinable<ISolution>, ITweakable
    {
        #region over

        public override string ToString()
        {
            string result = "";
            foreach (SimpleEntity e in data)
            {
                if (e.EntityName.StartsWith("A")) result += "A";
                else result += "B";
            }
            return result;
        }

        #endregion
        #region cvar

        private static Random rnd = new Random(123);

        private List<SimpleEntity> data;

        #endregion
        #region prop

        public IEnumerable<SimpleEntity> Data { get { return data; } }

        #region ISolution

        public double Fitness { get; set; }

        public bool HasFitness { get; set; }

        #endregion

        #endregion
        #region ctor

        public Solution(IEnumerable<SimpleEntity> data)
        {
            this.data = new List<SimpleEntity>();
            foreach (SimpleEntity s in data)
                this.data.Add(s);
        }

        #endregion
        #region impl

        #region IComparable

        public int CompareTo(ISolution other)
        {
            if (other == null)
                throw new ArgumentNullException("other");

            if (!HasFitness || !other.HasFitness)
                throw new ApplicationException("Cannot compare solutions: not fitness value available.");

            return Fitness.CompareTo(other.Fitness); 
        }

        #endregion
        #region ICloneable

        public object Clone()
        {
            Solution clone = new Solution(Data);
            clone.Fitness = Fitness;
            clone.HasFitness = HasFitness;
            return clone;
        }

        #endregion
        #region ITweakable

        public void Tweak()
        {
            int a = rnd.Next(data.Count);
            int b = rnd.Next(data.Count);

            SimpleEntity ea = data[a];
            SimpleEntity eb = data[b];
         
            data[a] = eb;
            data[b] = ea;

            HasFitness = false;
        }

        #endregion
        #region ICombinable<ISolution>

        public ISolution CombineWith(ISolution other)
        {
            if(this.HasFitness && other.HasFitness) 
            {
                if (Fitness > other.Fitness) return this;
                else return other;
            }
            else if(this.HasFitness) return this;
            else return other;
        }

        #endregion

        #endregion
    }
}
