using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Optimization.Interfaces;

namespace MatthiasToolbox.Optimization.Strategies.SimulatedAnnealing
{
    public interface ISimulatedAnnealingConfiguration : IConfiguration
    {
        double InitialTemperature { get; set; }
        IBrownianOperator Brownian { get; set; }
        Func<AnnealingAlgorithm, double> DecreaseTemperature { get; set; }
    }
}
