using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Mathematics.Enum
{
    public class LengthUnit : Unit
    {
        #region stat

        public static LengthUnit Meters = new LengthUnit("Meters", "m", 1);
        public static LengthUnit Decimeters = new LengthUnit("Decimeters", "dm", 0.1);
        public static LengthUnit Centimeters = new LengthUnit("Centimeters", "dm", 0.01);
        public static LengthUnit Millimeters = new LengthUnit("Millimeters", "mm", 0.001);
        public static LengthUnit Micrometers = new LengthUnit("Micrometers", "µm", 0.0001);
        public static LengthUnit Nanometers = new LengthUnit("Nanometers", "nm", 0.0000001);
        public static LengthUnit Kilometers = new LengthUnit("Kilometers", "km", 1000);

        public static LengthUnit Inch = new LengthUnit("Inch", "in", 0.0254);
        public static LengthUnit Feet = new LengthUnit("Feet", "ft", 0.3048);
        public static LengthUnit Yards = new LengthUnit("Yards", "yd", 0.9144);

        public static LengthUnit Miles = new LengthUnit("Miles", "mi", 1609.344);
        public static LengthUnit SurveyMiles = new LengthUnit("SurveyMiles", "mi", 1609.3472);
        public static LengthUnit NauticalMiles = new LengthUnit("NauticalMiles", "NM", 1852);

        public static LengthUnit SI = Meters;

        #endregion
        #region ctor

        public LengthUnit(string name, string abbreviation, double toSiConversionFactor)
            : base("Length", name, abbreviation, toSiConversionFactor)
        { }

        #endregion
    }
}
