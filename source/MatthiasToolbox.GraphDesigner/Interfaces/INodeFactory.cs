using System.Windows;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System;

namespace MatthiasToolbox.GraphDesigner.Interfaces
{
    public interface INodeFactory
    {
        IVertex<Point> CreateVertex(IVertex<Point> template, Point position, Size? size);
        UIElement CreateUIElement(IVertex<Point> vertex, IVertex<Point> template);
    }
}
