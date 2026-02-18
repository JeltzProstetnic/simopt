using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using System.Linq.Expressions;

namespace MatthiasToolbox.Simulation
{
    public static class Extensions
    {
        #region impl

        #region random

        /// <summary>
        /// Returns a new random number generator based on the given distribution using
        /// a seed which will be automatically retrieved from the invoker.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seedSource"></param>
        /// <param name="distribution"></param>
        /// <param name="antithetic"></param>
        /// <returns></returns>
        public static Random<T> Random<T>(this ISeedSource seedSource, IDistribution<T> distribution, bool antithetic = false, bool nonStochastic = false)
        {
            return new Random<T>(seedSource, distribution, antithetic, nonStochastic);
        }

        #endregion
        #region logging

        /// <summary>
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        public static void Log(this object sender, string message, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log(sender, message, data.Concat(simData).ToArray());
        }

        /// <summary>
        /// CAUTION: if "MessageClass", "MessageClassName", 
        /// "Sender" or "Message" appears as key in data, 
        /// these will be replaced by the ones in data!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        public static void Log<T>(this object sender, string message, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log<T>(sender, message, data.Concat(simData).ToArray());
        }

        /// <summary>
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        public static void Log(this object sender, string message, Exception exception, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log(sender, message, exception, data.Concat(simData).ToArray());
        }

        /// <summary>
        /// CAUTION: if "MessageClass", "MessageClassName", 
        /// "Sender" or "Message" appears as key in data, 
        /// these will be replaced by the ones in data!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        public static void Log<T>(this object sender, string message, Exception exception, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log<T>(sender, message, exception, data.Concat(simData).ToArray());
        }

        /// <summary>
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        public static void Log(this object sender, Exception exception, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log(sender, exception, data.Concat(simData).ToArray());
        }

        /// <summary>
        /// CAUTION: if "MessageClass", "MessageClassName", 
        /// "Sender" or "Message" appears as key in data, 
        /// these will be replaced by the ones in data!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        public static void Log<T>(this object sender, Exception exception, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log<T>(sender, exception, data.Concat(simData).ToArray());
        }

        #endregion
        #region time conversion

        /// <summary>
        /// depending on the time unit, this will convert 
        /// the value from the engines internal double value
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this double d)
        {
            return Simulator.DateTimeConversionMethod.Invoke(d);
        }

        /// <summary>
        /// depending on the time unit, this will convert 
        /// the value from the engines internal double value
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static TimeSpan ToTimeSpan(this double d)
        {
            return Simulator.TimeSpanConversionMethod.Invoke(d);
        }

        /// <summary>
        /// depending on the time unit, this will convert 
        /// the value to the engines internal double value
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static double ToDouble(this DateTime dateTime)
        {
            return Simulator.TimeConversionMethod.Invoke(dateTime);
        }

        /// <summary>
        /// depending on the time unit, this will convert 
        /// the value from the engines internal double value
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static double ToDouble(this TimeSpan timeSpan)
        {
            return Simulator.DurationConversionMethod.Invoke(timeSpan);
        }

        #endregion
        
        #endregion
        #region util

        /// <summary>
        /// returns the later of the two dates
        /// </summary>
        /// <param name="date1"></param>
        /// <param name="date2"></param>
        /// <returns></returns>
        public static DateTime Max(DateTime date1, DateTime date2)
        {
            return new DateTime(Math.Max(date1.Ticks, date2.Ticks));
        }

        /// <summary>
        /// returns the earlier of the two dates
        /// </summary>
        /// <param name="date1"></param>
        /// <param name="date2"></param>
        /// <returns></returns>
        public static DateTime Min(DateTime date1, DateTime date2)
        {
            return new DateTime(Math.Min(date1.Ticks, date2.Ticks));
        }

        /// <summary>
        /// returns the larger of the two time spans
        /// </summary>
        /// <param name="date1"></param>
        /// <param name="date2"></param>
        /// <returns></returns>
        public static TimeSpan Max(TimeSpan time1, TimeSpan time2)
        {
            return new TimeSpan(Math.Max(time1.Ticks, time2.Ticks));
        }

        /// <summary>
        /// returns the smaller of the two time spans
        /// </summary>
        /// <param name="date1"></param>
        /// <param name="date2"></param>
        /// <returns></returns>
        public static TimeSpan Min(TimeSpan time1, TimeSpan time2)
        {
            return new TimeSpan(Math.Min(time1.Ticks, time2.Ticks));
        }
        
        #endregion

        #region TimeSpan

        public static double TicksToDouble(this TimeSpan value)
        {
            return value.Ticks;
        }

        public static TimeSpan TicksToTimeSpan(this double value)
        {
            return new TimeSpan((long)value);
        }

        public static double TicksToMilliseconds(this double value)
        {
            return new TimeSpan((long)value).TotalMilliseconds;
        }

        #endregion
        #region DateTime

        public static double TicksToDouble(this DateTime value)
        {
            return value.Ticks;
        }

        public static DateTime TicksToDateTime(this double value)
        {
            return new DateTime((long)value);
        }

        #endregion
    }
}
