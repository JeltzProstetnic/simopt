using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Mathematics.Enum
{
    public class AngleUnit : Unit
    {
        #region stat

        public static AngleUnit Degrees = new AngleUnit("Degrees", "°", 1);
        public static AngleUnit Radians = new AngleUnit("Radians", "rad.", 180 / Math.PI);
        public static AngleUnit Revolutions = new AngleUnit("Revolutions", "rev.", 360);
        public static AngleUnit ArcMinutes = new AngleUnit("ArcMinutes", "'", 1 / 60);
        public static AngleUnit ArcSeconds = new AngleUnit("ArcSeconds", "\"", 1 / 3600);
        public static AngleUnit MilliArcSeconds = new AngleUnit("MilliArcSeconds", "milliarcsec.", 1 / 3600000);
        public static AngleUnit MicroArcSeconds = new AngleUnit("MicroArcSeconds", "microarcsec.", 1 / 3600000000);
        public static AngleUnit SI = Degrees;

        #endregion
        #region ctor

        public AngleUnit(string name, string abbreviation, double toSiConversionFactor) 
            : base("Angle", name, abbreviation, toSiConversionFactor)
        { }

        #endregion
    }
}
