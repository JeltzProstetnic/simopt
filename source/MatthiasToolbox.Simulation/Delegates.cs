using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Simulation.Tools;

namespace MatthiasToolbox.Simulation
{
    public delegate void SimulationFinishedHandler(object sender, SimulationEventArgs e);

    public delegate bool ResourcesReadyDelegate(Dictionary<Type, List<IResource>> resources);
    public delegate bool ResourceAcceptableDelegate(IResource resource);

    public delegate DateTime DateTimeConversionDelegate(double pointInTime);
    public delegate TimeSpan TimeSpanConversionDelegate(double timeSpan);
    public delegate double TimeConversionDelegate(DateTime pointInTime);
    public delegate double DurationConversionDelegate(TimeSpan timeSpan);
}
