using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Simulation
{
    /// <summary>
    /// simulation info logging message class
    /// </summary>
    public class SIM_INFO { static SIM_INFO() { MatthiasToolbox.Logging.Logger.RegisterMessageClass(typeof(SIM_INFO), 1); } }

    /// <summary>
    /// simulation info logging message class
    /// </summary>
    public class SIM_WARNING { static SIM_WARNING() { MatthiasToolbox.Logging.Logger.RegisterMessageClass(typeof(SIM_WARNING), 2); } }

    /// <summary>
    /// simulation info logging message class
    /// </summary>
    public class SIM_ERROR { static SIM_ERROR() { MatthiasToolbox.Logging.Logger.RegisterMessageClass(typeof(SIM_ERROR), 3); } }

    /// <summary>
    /// simulation info logging message class
    /// </summary>
    public class SIM_FATAL { static SIM_FATAL() { MatthiasToolbox.Logging.Logger.RegisterMessageClass(typeof(SIM_FATAL), 4); } }

    /// <summary>
    /// simulation event logging message class
    /// </summary>
    public class EVENT { static EVENT() { MatthiasToolbox.Logging.Logger.RegisterMessageClass(typeof(EVENT)); } }

    /// <summary>
    /// global code and data
    /// </summary>
    public static class Simulator
    {
        // logging tag name for the current simulation time
        public static readonly string SimTime = "SimTime";

        // methods for the conversion between double, DateTime and TimeSpan
        public static DateTimeConversionDelegate DateTimeConversionMethod = TimeFromDoubleTicks;
        public static TimeSpanConversionDelegate TimeSpanConversionMethod = DurationFromDoubleTicks;
        public static TimeConversionDelegate TimeConversionMethod = FromDateTimeTicks;
        public static DurationConversionDelegate DurationConversionMethod = FromTimeSpanTicks;
        
        public static readonly DefaultLoggerSettings LoggerSettings = 
            new DefaultLoggerSettings(typeof(SIM_INFO),
                                      typeof(SIM_WARNING),
                                      typeof(SIM_ERROR),
                                      typeof(SIM_FATAL), 
                                      typeof(EVENT));

        /// <summary>
        /// noop yet
        /// </summary>
        static Simulator() 
        {
        }

        /// <summary>
        /// registers a logger for the simulation logging classes
        /// </summary>
        /// <param name="logger"></param>
        public static void RegisterSimulationLogger(ILogger logger)
        {
            MatthiasToolbox.Logging.Logger.Add(logger, LoggerSettings);
        }
        
        // TODO: other time unit conversion methods will be implemented when needed
        
        private static DateTime TimeFromDoubleTicks(double d) { return new DateTime((long)d); }

        private static TimeSpan DurationFromDoubleTicks(double d) { return new TimeSpan((long)d); }

        private static double FromDateTimeTicks(DateTime pointInTime) { return (double)pointInTime.Ticks; }

        private static double FromTimeSpanTicks(TimeSpan timeSpan) { return (double)timeSpan.Ticks; }
    }
}
