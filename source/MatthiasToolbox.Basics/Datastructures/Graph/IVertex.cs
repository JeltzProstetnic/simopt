using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MatthiasToolbox.Basics.Interfaces;
using System.Windows;

namespace MatthiasToolbox.Basics.Datastructures.Graph
{
    public interface IVertex<TPoint> : INamedElement, IPosition<TPoint>
    {
    }
}