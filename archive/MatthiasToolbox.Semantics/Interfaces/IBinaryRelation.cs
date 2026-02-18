using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Semantics.Utilities;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;

namespace MatthiasToolbox.Semantics.Interfaces
{
    /// <summary>
    /// A binary relation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IBinaryRelation<T> : IRelation<T>, IEdge<Point>
        where T : IVertex<Point>
    {
        /// <summary>
        /// The owner of this property / relation. This corresponds to the first CardinalityValue in the Cardinality.
        /// </summary>
        T Owner { get; }

        /// <summary>
        /// The related entity. This corresponds to the second CardinalityValue in the Cardinality.
        /// </summary>
        T RelatedEntity { get; }

        /// <summary>
        /// The cardinality of this relation.
        /// The first value corresponds to the owner, the second one to the related entity.
        /// </summary>
        Cardinality Cardinality { get; }

        /// <summary>
        /// Get the cardinality value corresponding to the given entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        CardinalityValue GetCardinalityValue(T entity);
    }
}