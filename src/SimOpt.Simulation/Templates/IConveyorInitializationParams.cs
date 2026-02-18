using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Entities;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Interfaces;

namespace SimOpt.Simulation.Templates
{
    public interface IConveyorInitializationParams : IStateMachineEntityInitializationParams
    {
        double VMax { get; set; }

        double Acceleration { get; set; }

        double Deceleration { get; set; }

        double Length { get; set; }

        double PositioningTolerance { get; set; }

        int NumberOfSections { get; set; }

        double FirstSectionOffset { get; set; }

        Vector Orientation { get; set; }

        Vector AbsoluteOffset { get; set; }

        IMovable MobileElement { get; set; }
    }
}
