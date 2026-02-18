using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Threading;
using System.Collections.Concurrent;

namespace SimOpt.Logging
{
    /// <summary>
    /// object extension methods for logging plus an 
    /// extension method for ILogger to find its settings
    /// </summary>
    public static class Extensions
    {
        #region ILogger

        /// <summary>
        /// will return null if the given logger cannot be found.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static ILoggerSettings GetSettings(this ILogger logger) 
        {
            if (Logger.loggers.ContainsKey(logger)) return Logger.loggers[logger];
            return null;
        }

        #endregion
        #region Object

        /// <summary>
        /// Enqueue or log a message.
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="sender">The log message sender</param>
        /// <param name="message">The message to log</param>
        /// <param name="data">further data coded as Key => Value expressions
        /// example: <code>Log("Hello World!", Weather => "Nice");</code></param>
        public static void Log(this object sender, string message, params Expression<Func<object, object>>[] data) 
        {
            Dictionary<string, object> tmp = new Dictionary<string, object>();
            
            tmp[Logger.Sender] = sender;
            tmp[Logger.Message] = message;
            tmp[Logger.TimeStamp] = DateTime.Now;
            tmp[Logger.MessageClass] = Logger.DefaultMessageClass;
            tmp[Logger.MessageClassName] = Logger.DefaultMessageClass.Name;

            if (Logger.UseMessageClassAsSeverity && Logger.MessageClassSeverity.ContainsKey(Logger.DefaultMessageClass))
                tmp[Logger.Severity] = Logger.MessageClassSeverity[Logger.DefaultMessageClass];
            else
                tmp[Logger.Severity] = 0f;
            
            foreach (Expression<Func<object, object>> e in data)
            {
                tmp[e.Parameters[0].Name] = e.Compile().Invoke(null);
            }
            
            Logger.Enqueue(tmp);
        }

        /// <summary>
        /// Enqueue or log a message.
        /// CAUTION: if "MessageClass", "MessageClassName", 
        /// "Sender" or "Message" appears as key in data, 
        /// these will be replaced by the ones in data!
        /// </summary>
        /// <typeparam name="T">The message class</typeparam>
        /// <param name="sender">The log message sender</param>
        /// <param name="message">The message to log</param>
        /// <param name="data">further data coded as Key => Value expressions
        /// example: <code>Log("Hello World!", Weather => "Nice");</code></param>
        public static void Log<T>(this object sender, string message, params Expression<Func<object, object>>[] data) 
        {
            Dictionary<string, object> tmp = new Dictionary<string, object>();
            
            tmp[Logger.Sender] = sender;
            tmp[Logger.Message] = message;
            tmp[Logger.TimeStamp] = DateTime.Now;
            tmp[Logger.MessageClass] = typeof(T);
            tmp[Logger.MessageClassName] = typeof(T).Name;
            
            if (Logger.UseMessageClassAsSeverity && Logger.MessageClassSeverity.ContainsKey(typeof(T)))
                tmp[Logger.Severity] = Logger.MessageClassSeverity[typeof(T)];
            else
                tmp[Logger.Severity] = 0f;

            foreach (Expression<Func<object, object>> e in data)
            {
                tmp[e.Parameters[0].Name] = e.Compile().Invoke(null);
            }
            
            Logger.Enqueue(tmp);
        }

        /// <summary>
        /// Enqueue or log a message.
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="sender">The log message sender</param>
        /// <param name="message">The message to log</param>
        /// <param name="exception">The exception related to this message</param>
        /// <param name="data">further data coded as Key => Value expressions
        /// example: <code>Log("Hello World!", Weather => "Nice");</code></param>
        public static void Log(this object sender, string message, Exception exception, params Expression<Func<object, object>>[] data)
        {
            string msg;
            if (Logger.ConcatExceptionMessage)
                msg = message + Logger.ConcatExceptionMessageSeparator + exception.Message;
            else
                msg = message;
            Dictionary<string, object> tmp = new Dictionary<string, object>();

            tmp[Logger.Sender] = sender;
            tmp[Logger.Message] = msg;
            tmp[Logger.TimeStamp] = DateTime.Now;
            tmp[Logger.ExceptionS] = exception;
            tmp[Logger.MessageClass] = Logger.DefaultExceptionClass;
            tmp[Logger.MessageClassName] = Logger.DefaultExceptionClass.Name;

            if (Logger.UseMessageClassAsSeverity && Logger.MessageClassSeverity.ContainsKey(Logger.DefaultExceptionClass))
                tmp[Logger.Severity] = Logger.MessageClassSeverity[Logger.DefaultExceptionClass];
            else
                tmp[Logger.Severity] = 0f;

            foreach (Expression<Func<object, object>> e in data)
            {
                tmp[e.Parameters[0].Name] = e.Compile().Invoke(null);
            }

            Logger.Enqueue(tmp);
        }

        /// <summary>
        /// Enqueue or log a message.
        /// CAUTION: if "MessageClass", "MessageClassName", 
        /// "Sender" or "Message" appears as key in data, 
        /// these will be replaced by the ones in data!
        /// </summary>
        /// <typeparam name="T">The message class</typeparam>
        /// <param name="sender">The log message sender</param>
        /// <param name="message">The message to log</param>
        /// <param name="exception">The exception related to this message</param>
        /// <param name="data">further data coded as Key => Value expressions
        /// example: <code>Log("Hello World!", Weather => "Nice");</code></param>
        public static void Log<T>(this object sender, string message, Exception exception, params Expression<Func<object, object>>[] data)
        {
            string msg;
            if (Logger.ConcatExceptionMessage)
                msg = message + Logger.ConcatExceptionMessageSeparator + exception.Message;
            else
                msg = message;
            Dictionary<string, object> tmp = new Dictionary<string, object>();

            tmp[Logger.Sender] = sender;
            tmp[Logger.Message] = msg;
            tmp[Logger.TimeStamp] = DateTime.Now;
            tmp[Logger.ExceptionS] = exception;
            tmp[Logger.MessageClass] = typeof(T);
            tmp[Logger.MessageClassName] = typeof(T).Name;

            if (Logger.UseMessageClassAsSeverity && Logger.MessageClassSeverity.ContainsKey(typeof(T)))
                tmp[Logger.Severity] = Logger.MessageClassSeverity[typeof(T)];
            else
                tmp[Logger.Severity] = 0f;

            foreach (Expression<Func<object, object>> e in data)
            {
                tmp[e.Parameters[0].Name] = e.Compile().Invoke(null);
            }

            Logger.Enqueue(tmp);
        }

        /// <summary>
        /// Enqueue or log a message.
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="sender">The log message sender</param>
        /// <param name="exception">The exception related to this message</param>
        /// <param name="data">further data coded as Key => Value expressions
        /// example: <code>Log("Hello World!", Weather => "Nice");</code></param>
        public static void Log(this object sender, Exception exception, params Expression<Func<object, object>>[] data)
        {
            Dictionary<string, object> tmp = new Dictionary<string, object>();

            tmp[Logger.Sender] = sender;
            tmp[Logger.Message] = exception.Message;
            tmp[Logger.TimeStamp] = DateTime.Now;
            tmp[Logger.ExceptionS] = exception;
            tmp[Logger.MessageClass] = Logger.DefaultExceptionClass;
            tmp[Logger.MessageClassName] = Logger.DefaultExceptionClass.Name;

            if (Logger.UseMessageClassAsSeverity && Logger.MessageClassSeverity.ContainsKey(Logger.DefaultExceptionClass))
                tmp[Logger.Severity] = Logger.MessageClassSeverity[Logger.DefaultExceptionClass];
            else
                tmp[Logger.Severity] = 0f;

            foreach (Expression<Func<object, object>> e in data)
            {
                tmp[e.Parameters[0].Name] = e.Compile().Invoke(null);
            }

            Logger.Enqueue(tmp);
        }

        /// <summary>
        /// Enqueue or log a message.
        /// CAUTION: if "MessageClass", "MessageClassName", 
        /// "Sender" or "Message" appears as key in data, 
        /// these will be replaced by the ones in data!
        /// </summary>
        /// <typeparam name="T">The message class</typeparam>
        /// <param name="sender">The log message sender</param>
        /// <param name="exception">The exception related to this message</param>
        /// <param name="data">further data coded as Key => Value expressions
        /// example: <code>Log("Hello World!", Weather => "Nice");</code></param>
        public static void Log<T>(this object sender, Exception exception, params Expression<Func<object, object>>[] data)
        {
            Dictionary<string, object> tmp = new Dictionary<string, object>();

            tmp[Logger.Sender] = sender;
            tmp[Logger.Message] = exception.Message;
            tmp[Logger.TimeStamp] = DateTime.Now;
            tmp[Logger.ExceptionS] = exception;
            tmp[Logger.MessageClass] = typeof(T);
            tmp[Logger.MessageClassName] = typeof(T).Name;

            if (Logger.UseMessageClassAsSeverity && Logger.MessageClassSeverity.ContainsKey(typeof(T)))
                tmp[Logger.Severity] = Logger.MessageClassSeverity[typeof(T)];
            else
                tmp[Logger.Severity] = 0f;

            foreach (Expression<Func<object, object>> e in data)
            {
                tmp[e.Parameters[0].Name] = e.Compile().Invoke(null);
            }

            Logger.Enqueue(tmp);
        }

        #endregion
    }
}
