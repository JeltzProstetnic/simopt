using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Logging
{
    /// <summary>
    /// Interface for log message consumer classes
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// may be set by the logging framework to suppress logging
        /// </summary>
        bool LoggingEnabled { get; set; }

        /// <summary>
        /// log the given message
        /// </summary>
        /// <param name="timeStamp">the timestamp for this message</param>
        /// <param name="sender">the message origin</param>
        /// <param name="messageClass">the message class</param>
        /// <param name="message">the message</param>
        /// <param name="data">additional data</param>
        void Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data);
        
        /// <summary>
        /// cease logging
        /// </summary>
        void ShutdownLogger();
    }
}
