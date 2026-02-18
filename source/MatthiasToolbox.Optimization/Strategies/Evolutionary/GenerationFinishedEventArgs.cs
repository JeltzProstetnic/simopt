using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Optimization.Interfaces;

namespace MatthiasToolbox.Optimization.Strategies.Evolutionary
{
    public class GenerationFinishedEventArgs : EventArgs
    {
        public IEnumerable<ISolution> NewGeneration { get; private set; }

        public GenerationFinishedEventArgs(IEnumerable<ISolution> newGeneration)
        {
            this.NewGeneration = newGeneration;
        }
    }
}
