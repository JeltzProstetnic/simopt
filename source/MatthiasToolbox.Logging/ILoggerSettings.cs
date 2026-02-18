using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Logging
{
    /// <summary>
    /// interface for log message filters
    /// </summary>
    public interface ILoggerSettings
    {
        /// <summary>
        /// return true if this data is to be processed by the logger.
        /// </summary>
        /// <param name="data">the message data</param>
        /// <returns>true if the data fits the criteria to be logged</returns>
        bool ProcessFilters(Dictionary<string, object> data);
    }
}
