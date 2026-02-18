using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Mathematics.Enum;

namespace MatthiasToolbox.Mathematics.Enum
{
    public class TimeUnit : Unit
    {
        #region stat

        public static TimeUnit Attoseconds = new TimeUnit("Attoseconds", "as", 0.000000000000000001);
        public static TimeUnit Femtoseconds = new TimeUnit("Femtoseconds", "fs", 0.000000000000001);
        public static TimeUnit Picoseconds = new TimeUnit("Picoseconds", "ps", 0.000000000001);
        public static TimeUnit Nanoseconds = new TimeUnit("Nanoseconds", "ns", 0.000000001);
        public static TimeUnit Ticks = new TimeUnit("Ticks", "ticks", 0.0000001);
        public static TimeUnit Microseconds = new TimeUnit("Microseconds", "µs", 0.000001);
        public static TimeUnit Milliseconds = new TimeUnit("Milliseconds", "ms", 0.001);
        public static TimeUnit Seconds = new TimeUnit("Seconds", "\"", 1);
        public static TimeUnit Minutes = new TimeUnit("Minutes", "\'", 60);
        public static TimeUnit Hours = new TimeUnit("Hours", "h", 3600);
        public static TimeUnit Days = new TimeUnit("Days", "d", 86400);
        public static TimeUnit Weeks = new TimeUnit("Weeks", "w", 604800);

        public static TimeUnit SI = Seconds;

        #endregion
        #region ctor

        public TimeUnit(string name, string abbreviation, double toSiConversionFactor) 
            : base("Time", name, abbreviation, toSiConversionFactor)
        { }

        #endregion
    }
}
