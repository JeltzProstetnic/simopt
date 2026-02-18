using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Events;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Basics.Interfaces;

namespace SimOpt.Simulation.Interfaces
{
    public interface IMovable : IPosition<Point>
    {
        BinaryEvent<IMovable, Point> StoppedEvent { get; }

        void MoveTo(Point position);

        void StartMoving(Vector direction);

        void StopMoving();
    }
}