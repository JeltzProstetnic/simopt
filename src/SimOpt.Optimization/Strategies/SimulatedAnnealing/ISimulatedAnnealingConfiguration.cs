using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.Optimization.Strategies.SimulatedAnnealing
{
    public interface ISimulatedAnnealingConfiguration : IConfiguration
    {
        double InitialTemperature { get; set; }
        IBrownianOperator Brownian { get; set; }
        Func<AnnealingAlgorithm, double> DecreaseTemperature { get; set; }
    }
}
