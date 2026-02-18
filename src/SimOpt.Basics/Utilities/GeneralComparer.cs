using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Utilities
{
    /// <summary>
    /// General comparer which supports multiple directions
    /// and comparison of absolute values.
    /// </summary>
    public class GeneralComparer : IComparer<double>
    {
        private bool absolute;
        private int direction = 1;

        /// <summary>
        ///   Constructs a new General Comparer.
        /// </summary>
        /// <param name="direction">The direction to compare.</param>
        /// <param name="useAbsoluteValues">True to compare absolute values, false otherwise.</param>
        public GeneralComparer(ComparerDirection direction, bool useAbsoluteValues)
        {
            this.direction = (direction == ComparerDirection.Ascending) ? 1 : -1;
            this.absolute = useAbsoluteValues;
        }

        /// <summary>
        ///   Compares two objects and returns a value indicating whether one is less than,
        ///    equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        public int Compare(double x, double y)
        {
            if (absolute)
            {
                return direction * (System.Math.Abs(x).CompareTo(System.Math.Abs(y)));
            }
            else
            {
                return direction * (x.CompareTo(y));
            }
        }
    }
}
