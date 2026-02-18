using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;

namespace MatthiasToolbox.Semantics.Interfaces
{
    /// <summary>
    /// Part of a parent-child relation (either parentOf or childOf).
    /// non circular!, transitive
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHierarchyRelation<T> : IBinaryRelation<T>
        where T : IVertex<Point>
    {
        T Parent { get; }
        T Child { get; }
    }
}