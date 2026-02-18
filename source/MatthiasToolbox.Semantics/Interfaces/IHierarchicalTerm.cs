using System.Collections.Generic;
using MatthiasToolbox.Semantics.Utilities;

namespace MatthiasToolbox.Semantics.Interfaces
{
    public interface IHierarchicalTerm : ITerm, IHierarchyElement<IHierarchicalTerm>
    {
        /// <summary>
        /// Gets the hyperonym = parent.
        /// </summary>
        /// <value>The hyperonym = parent.</value>
        IHierarchicalTerm Hyperonym { get; }

        /// <summary>
        /// Gets the hyponyms = children.
        /// </summary>
        /// <value>The hyponyms = children.</value>
        IEnumerable<IHierarchicalTerm> Hyponyms { get; }

        /// <summary>
        /// Gets the relation type for the hierarchy.
        /// </summary>
        /// <value>The child of relation.</value>
        string HierarchyType { get; }
    }
}