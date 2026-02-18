using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Basics.Interfaces;
using System.Windows;

namespace MatthiasToolbox.Semantics.Interfaces
{
    public interface IBinaryRelationPartner<T>
        where T : IVertex<Point>
    {
        /// <summary>
        /// All relations which are owned by this term.
        /// </summary>
        IEnumerable<IBinaryRelation<T>> Relations { get; }

        /// <summary>
        /// All terms which are related to this instance.
        /// </summary>
        IEnumerable<T> RelatedItems { get; }

        /// <summary>
        /// TODO  Semantics - comment, extend for remove, find etc.
        /// </summary>
        /// <param name="relation"></param>
        /// <returns></returns>
        bool AddRelation(IBinaryRelation<T> relation);

        /// <summary>
        /// Gets the relations of the given RelationTypes.
        /// </summary>
        /// <param name="relationType">Type of the relation.</param>
        /// <returns></returns>
        IEnumerable<IBinaryRelation<T>> GetRelations(INamedElement relationType);

        /// <summary>
        /// Gets the relations of the given RelationTypes.
        /// </summary>
        /// <param name="relationType">Type of the relation.</param>
        /// <returns></returns>
        IEnumerable<T> GetRelatedItems(INamedElement relationType);
    }
}