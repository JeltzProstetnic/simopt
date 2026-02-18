using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Logging
{
    public class DefaultLoggerSettings : ILoggerSettings
    {
        #region cvar

        private bool logIfNoSeverityGiven = true;
        private float minimumSeverity = 0;
        private List<Type> messageClassWhiteList;
        private Func<Dictionary<string, object>, bool> evaluationMethod;

        #endregion
        #region ctor

        /// <summary>
        /// log everything with severity > 0 or no given severity
        /// </summary>
        public DefaultLoggerSettings() 
        { 
            // evaluationMethod = data => true; 
            this.minimumSeverity = 1f;
            evaluationMethod = MinimumSeverityEvaluator;
        }

        /// <summary>
        /// log only messages with message class contained in the given list
        /// </summary>
        /// <param name="messageTypeWhiteList">list of allowed message classes</param>
        public DefaultLoggerSettings(params Type[] messageTypeWhiteList) 
        {
            messageClassWhiteList = new List<Type>(messageTypeWhiteList);
            evaluationMethod = WhiteListEvaluator; 
        }

        /// <summary>
        /// log only messages which pass through the given filter
        /// </summary>
        /// <param name="filter">only messages on which the filter method returns true will be logged</param>
        public DefaultLoggerSettings(Func<Dictionary<string, object>, bool> filter) 
        {
            evaluationMethod = filter;
        }

        /// <summary>
        /// log only messages with a given minimum severity.
        /// </summary>
        /// <param name="minimumSeverity">the minimum severity for messages to be accepted</param>
        /// <param name="logIfNoSeverityGiven">if set to false, messages without a severity will not be logged</param>
        public DefaultLoggerSettings(float minimumSeverity, bool logIfNoSeverityGiven = false) 
        {
            this.minimumSeverity = minimumSeverity;
            this.logIfNoSeverityGiven = logIfNoSeverityGiven;
            evaluationMethod = MinimumSeverityEvaluator;
        }

        #endregion
        #region impl

        private bool WhiteListEvaluator(Dictionary<string, object> data)
        {
            return data["MessageClass"] is Type && messageClassWhiteList.Contains((Type)data["MessageClass"]);
        }

        private bool MinimumSeverityEvaluator(Dictionary<string, object> data)
        {
            if (data.ContainsKey("Severity") && 
                ((data["Severity"] as float?).HasValue ||
                (data["Severity"] as int?).HasValue ||
                (data["Severity"] as double?).HasValue))
                return (float)data["Severity"] >= minimumSeverity;
            else
                return logIfNoSeverityGiven;
        }

        /// <summary>
        /// process the message and return true if it fits the criteria
        /// </summary>
        /// <param name="data">the message data</param>
        /// <returns>true if the data fits the criteria to be logged</returns>
        public bool ProcessFilters(Dictionary<string, object> data) 
        {
            return evaluationMethod.Invoke(data);
        }

        #endregion
    }
}
