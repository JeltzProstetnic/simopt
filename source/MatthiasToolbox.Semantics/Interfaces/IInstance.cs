using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;

namespace MatthiasToolbox.Semantics.Interfaces
{
    public interface IInstance :
        IVertex<Point>,
        INamedElement
    {
        /// <summary>
        /// The ontology in which this instance is contained.
        /// </summary>
        IOntology Container { get; }

        /// <summary>
        /// The concept of which this is an instance.
        /// </summary>
        IConcept Concept { get; }

        /// <summary>
        /// Retrieve the value of a property. This will either return a 
        /// concept or a literal value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        T GetProperty<T>(string name);

        IInstance GetRelatedInstance(INamedElement relationType);

        IInstance GetParentInstance(INamedElement hierarchyType);

        IEnumerable<IInstance> GetChildInstances(INamedElement hierarchyType);
    }
}