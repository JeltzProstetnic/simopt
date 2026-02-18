using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Semantics.Utilities
{
    public class Cardinality : Tuple<CardinalityValue, CardinalityValue>
    {
        #region over

        public override string ToString()
        {
            return "[" + Item1.MinValue.ToString() + ".." + Item1.MaxValue.ToString() + "] - [" +
                Item2.MinValue.ToString() + ".." + Item2.MaxValue.ToString() + "]";
        }

        #endregion
        #region cvar

        /// <summary>
        /// [1] - [0..n]
        /// </summary>
        public static readonly Cardinality ExactlyOneToAny = new Cardinality(CardinalityValue.ExactlyOne, CardinalityValue.N);

        /// <summary>
        /// [1] - [1..n]
        /// </summary>
        public static readonly Cardinality ExactlyOneToMany = new Cardinality(CardinalityValue.ExactlyOne, CardinalityValue.AtLeastOne);

        /// <summary>
        /// [1] - [0..1]
        /// </summary>
        public static readonly Cardinality ExactlyOneToOne = new Cardinality(CardinalityValue.ExactlyOne, CardinalityValue.One);

        /// <summary>
        /// [1] - [1]
        /// </summary>
        public static readonly Cardinality ExactlyOneToExactlyOne = new Cardinality(CardinalityValue.ExactlyOne, CardinalityValue.ExactlyOne);

        /// <summary>
        /// [0..1] - [0..n]
        /// </summary>
        public static readonly Cardinality OneToAny = new Cardinality(CardinalityValue.One, CardinalityValue.N);

        /// <summary>
        /// [0..1] - [1..n]
        /// </summary>
        public static readonly Cardinality OneToMany = new Cardinality(CardinalityValue.One, CardinalityValue.AtLeastOne);

        /// <summary>
        /// [0..1] - [0..1]
        /// </summary>
        public static readonly Cardinality OneToOne = new Cardinality(CardinalityValue.One, CardinalityValue.One);

        /// <summary>
        /// [0..1] - [1]
        /// </summary>
        public static readonly Cardinality OneToExactlyOne = new Cardinality(CardinalityValue.One, CardinalityValue.ExactlyOne);

        /// <summary>
        /// [1..n] - [1..n]
        /// </summary>
        public static readonly Cardinality ManyToMany = new Cardinality(CardinalityValue.AtLeastOne, CardinalityValue.AtLeastOne);

        /// <summary>
        /// [1..n] - [0..n]
        /// </summary>
        public static readonly Cardinality ManyToAny = new Cardinality(CardinalityValue.AtLeastOne, CardinalityValue.N);

        /// <summary>
        /// [0..n] - [0..n]
        /// </summary>
        public static readonly Cardinality AnyToAny = new Cardinality(CardinalityValue.N, CardinalityValue.N);

        /// <summary>
        /// [0..n] - [1..n]
        /// </summary>
        public static readonly Cardinality AnyToMany = new Cardinality(CardinalityValue.N, CardinalityValue.AtLeastOne);

        /// <summary>
        /// [1..n] - [1]
        /// </summary>
        public static readonly Cardinality ManyToExactlyOne = new Cardinality(CardinalityValue.AtLeastOne, CardinalityValue.ExactlyOne);

        /// <summary>
        /// [1..n] - [0..1]
        /// </summary>
        public static readonly Cardinality ManyToOne = new Cardinality(CardinalityValue.AtLeastOne, CardinalityValue.One);

        /// <summary>
        /// [0..n] - [1]
        /// </summary>
        public static readonly Cardinality AnyToExactlyOne = new Cardinality(CardinalityValue.N, CardinalityValue.ExactlyOne);

        /// <summary>
        /// [0..n] - [0..1]
        /// </summary>
        public static readonly Cardinality AnyToOne = new Cardinality(CardinalityValue.N, CardinalityValue.One);

        #endregion
        #region ctor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        public Cardinality(CardinalityValue sourceValue, CardinalityValue targetValue) : base(sourceValue, targetValue) { }

        #endregion
    }
}