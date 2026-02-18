using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SimOpt.Basics.Interfaces;


namespace SimOpt.Basics.Datastructures.Graph
{
    public interface IVertex<TPoint> : INamedElement, IPosition<TPoint>
    {
    }
}