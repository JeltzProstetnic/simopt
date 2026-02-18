using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;

namespace MatthiasToolbox.Semantics.Interfaces
{
    /// <summary>
    /// A binary relation of IConcept which represents a property of the owner concept.
    /// </summary>
    /// <typeparam name="T">The type of the related entities.</typeparam>
    public interface IPropertyRelation<T> : IBinaryRelation<T>
        where T : IVertex<Point>
    {
        /// <summary>
        /// The data type of the property. This may be a primitive type in case of a literal property.
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// The property value.
        /// </summary>
        T Value { get; set; }
    }
}