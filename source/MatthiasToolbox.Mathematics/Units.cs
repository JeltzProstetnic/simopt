using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Mathematics.Enum;

namespace MatthiasToolbox.Mathematics
{
    public static class Units
    {
        #region impl

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fromUnit"></param>
        /// <param name="value"></param>
        /// <param name="toUnit"></param>
        /// <returns></returns>
        public static double Convert(double value, Unit fromUnit, Unit toUnit)
        {
            if (fromUnit.GetType() != toUnit.GetType()) throw new ArgumentException("Cannot convert " + fromUnit.TypeName + " to " + toUnit.TypeName + ".");
            return value * (fromUnit.ToSiConversionFactor * toUnit.FromSiConversionFactor);
        }

        #region direct conversions

        public static double DegreeToRadians(double v)
        {
            return v * Math.PI / 180.0;
        }

        public static double RadiansToDegree(double v)
        {
            return v * 180.0 / Math.PI;
        }

        /// <summary>
        /// for negative angles, d must be negative, m & s positive.
        /// </summary>
        /// <param name="d">positive or negative</param>
        /// <param name="m">provide only positive values!</param>
        /// <param name="s">provide only positive values!</param>
        /// <returns></returns>
        public static double DmsToRadians(double d, double m, double s)
        {
            if (d >= 0) return (d + m / 60 + s / 3600) * Math.PI / 180.0;
            return (d - m / 60 - s / 3600) * Math.PI / 180.0;
        }

        /// <summary>
        /// for negative angles, d must be negative, m & s positive.
        /// </summary>
        /// <param name="d">positive or negative</param>
        /// <param name="m">provide only positive values!</param>
        /// <param name="s">provide only positive values!</param>
        /// <returns></returns>
        public static double DmsToDegree(double d, double m, double s)
        {
            if (d >= 0) return (d + m / 60 + s / 3600);
            return (d - m / 60 - s / 3600);
        }

        #endregion

        #endregion
    }
}
