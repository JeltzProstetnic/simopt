using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Semantics.Utilities;

namespace MatthiasToolbox.Semantics.Interfaces
{
    /// <summary>
    /// This may represent any n-ary relation.
    /// 
    /// TODO: semantic relation properties:
    /// A relation which is reflexive, symmetric and transitive is called an equivalence relation. 
    /// A relation which is reflexive, antisymmetric and transitive is called a partial order. 
    /// A partial order which is total is called a total order, simple order, linear order or a chain.
    /// A linear order in which every nonempty set has a least element is called a well-order. 
    /// A relation which is symmetric, transitive, and serial is also reflexive.
    /// bijective: left-total, right-total, functional, and injective. A bijective relation is sometimes called a 1-to-1 correspondence
    /// </summary>
    /// <typeparam name="T">The common type of all related entities.</typeparam>
    public interface IRelation<T> : INamedElement
    {
        #region prop

        #region main

        /// <summary>
        /// Relation type.
        /// </summary>
        string RelationType { get; }

        /// <summary>
        /// All items which this relation connects.
        /// </summary>
        IEnumerable<T> Members { get; }

        #endregion
        #region mathematical properties

        /// <summary>
        /// for all x in X there exists a y in Y such that xRy
        /// </summary>
        bool IsLeftTotal { get; set; }

        /// <summary>
        /// for all y in Y there exists an x in X such that xRy
        /// synonym: surjective
        /// </summary>
        bool IsRightTotal { get; set; }
        bool IsSurjective { get; set; }

        /// <summary>
        /// for all x in X, and y and z in Y it holds that if xRy and xRz then y = z
        /// synonymes: right-unique
        /// </summary>
        bool IsRightUnique { get; set; }
        bool IsFunctional { get; set; }

        /// <summary>
        /// for all x and z in X and y in Y it holds that if xRy and zRy then x = z
        /// synonyme: left-unique
        /// </summary>
        bool IsLeftUnique { get; set; }
        bool IsInjective { get; set; }

        /// <summary>
        /// for all x in X: xRx
        /// Every reflexive relation is serial
        /// </summary>
        bool IsReflexive { get; set; }

        /// <summary>
        /// for all x in X: !xRx
        /// synonym: strict?
        /// </summary>
        bool IsIrreflexive { get; set; }
        bool IsStrict { get; set; }

        /// <summary>
        /// for all x, y in X: xRy => x = y
        /// </summary>
        bool IsCoreflexive { get; set; }

        /// <summary>
        /// for all x and y in X it holds that if xRy then yRx
        /// </summary>
        bool IsSymmetric { get; set; }

        /// <summary>
        /// for all x and y in X it holds that if xRy and yRx then x = y
        /// </summary>
        bool IsAntiSymmetric { get; set; }

        /// <summary>
        /// for all x and y in X it holds that if xRy then not yRx.
        /// </summary>
        bool IsAsymmetric { get; set; }

        /// <summary>
        /// for all x, y and z in X it holds that if xRy and yRz then xRz
        /// </summary>
        bool IsTransitive { get; set; }

        /// <summary>
        /// for all x and y in X it holds that xRy or yRx (or both).
        /// </summary>
        bool IsTotal { get; set; }

        /// <summary>
        /// for all x and y in X exactly one of xRy, yRx or x = y holds
        /// </summary>
        bool IsTrichotomous { get; set; }

        /// <summary>
        /// for all x, y and z in X it holds that if xRy and xRz, then yRz (and zRy).
        /// </summary>
        bool IsEuclidean { get; set; }

        /// <summary>
        /// for all x in X, there exists y in X such that xRy. 
        /// Every reflexive relation is serial
        /// </summary>
        bool IsSerial { get; set; }

        #endregion

        #endregion
        #region impl

        //bool IsInstanceOf(string t);
        bool IsInstanceOf(String relationTypeName);

        #endregion
    }
}