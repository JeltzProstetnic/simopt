using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;
using System.Windows.Media;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Semantics.Interfaces
{
    public interface ISemanticNode : IVertex<Point>, ITitle, ISize<Size>, IColors<Color>
    {
    }
}
