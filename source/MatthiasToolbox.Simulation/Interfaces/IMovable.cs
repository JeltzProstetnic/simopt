using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Events;
using MatthiasToolbox.Basics.Datastructures.Geometry;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Simulation.Interfaces
{
    public interface IMovable : IPosition<Point>
    {
        BinaryEvent<IMovable, Point> StoppedEvent { get; }

        void MoveTo(Point position);

        void StartMoving(Vector direction);

        void StopMoving();
    }
}