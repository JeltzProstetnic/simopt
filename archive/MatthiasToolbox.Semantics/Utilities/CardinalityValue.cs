using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Semantics.Utilities
{
    /// <summary>
    /// This struct is made so that the default value is N (0..n).
    /// </summary>
    public struct CardinalityValue
    {
        #region cvar

        private int minValue;
        private int maxValue;

        #region constants

        /// <summary>
        /// 0..n
        /// </summary>
        public static readonly CardinalityValue N = new CardinalityValue(0, double.PositiveInfinity);

        /// <summary>
        /// 0..1
        /// </summary>
        public static readonly CardinalityValue One = new CardinalityValue(0, 1);
        
        /// <summary>
        /// 1..n
        /// </summary>
        public static readonly CardinalityValue AtLeastOne = new CardinalityValue(1, double.PositiveInfinity);
        
        /// <summary>
        /// 1
        /// </summary>
        public static readonly CardinalityValue ExactlyOne = new CardinalityValue(1, 1);

        #endregion

        #endregion
        #region prop

        public double MinValue
        {
            get
            {
                if (minValue == 0 && maxValue == 0) maxValue = -1;
                return minValue;
            }
            set 
            {
                if (minValue == 0 && maxValue == 0) maxValue = -1;

                if (value != Math.Truncate(value) || value < 0 || value >= int.MaxValue) 
                    throw new ArgumentOutOfRangeException("A cardinality minimum value must be a positive integer number.");
                
                // value is integer, >= 0 and < int.MaxValue

                if (maxValue != -1 && value > maxValue) throw new ArgumentOutOfRangeException("The minimum value must be smaller than the maximum value.");
                minValue = (int)value;
            }
        }

        public double MaxValue
        {
            get
            {
                if (minValue == 0 && maxValue == 0) maxValue = -1;
                return (maxValue == -1) ? double.PositiveInfinity : maxValue;
            }
            set
            {
                if (minValue == 0 && maxValue == 0) maxValue = -1;

                if (value != Math.Truncate(value) || (value != -1 && value <= 0) || (!double.IsPositiveInfinity(value) && value > int.MaxValue)) 
                    throw new ArgumentOutOfRangeException("A cardinality maximum value must be a positive integer number or PositiveInfinity.");
                
                // value is integer, > 0 and <= int.MaxValue or -1

                if (double.IsPositiveInfinity(value)) maxValue = -1;
                else if (value == -1) maxValue = -1;
                else
                {
                    if (value < minValue) throw new ArgumentOutOfRangeException("The maximum value must be greater than the minimum value.");
                    maxValue = (int)value;
                }
            }
        }

        #endregion
        #region ctor

        internal CardinalityValue(int min = 0, int max = -1) : this()
        {
            MinValue = min;
            MaxValue = max;
        }

        public CardinalityValue(double min = 0, double max = double.PositiveInfinity)
            : this()
        {
            MinValue = min;
            MaxValue = max;
        }

        #endregion
        #region impl

        public bool IsAllowed(int n) 
        {
            if (minValue == 0 && maxValue == 0) maxValue = -1;
            return (double)n >= MinValue && (double)n <= MaxValue;
        }

        #endregion
    }
}