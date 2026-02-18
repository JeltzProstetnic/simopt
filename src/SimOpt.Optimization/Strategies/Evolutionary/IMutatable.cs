using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.Optimization.Strategies.Evolutionary
{
    /// <summary>
    /// Interface for a mutatable class. Inherited from
    /// <code>IParametrizedTweakable<double></code>
    /// </summary>
    public interface IMutatable : IParametrizedTweakable<double>
    {
    }
}
