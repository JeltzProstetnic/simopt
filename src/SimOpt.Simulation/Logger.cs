using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation
{
    public static class Logger
    {
        /// <summary>
        /// force the loggers to finish writing their data.
        /// this will block the current thread until the
        /// logging has been finished.
        /// </summary>
        public static void Dispatch() 
        {
            SimOpt.Logging.Logger.Dispatch();
        }

        #region normal

        /// <summary>
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        public static void Log(string message, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log(message, data.Concat(simData).ToArray());
        }

        /// <summary>
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        public static void Log(object sender, string message, IModel model, params Expression<Func<object, object>>[] data)
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
        public static void Log<T>(string message, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log<T>(message, data.Concat(simData).ToArray());
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
        public static void Log<T>(object sender, string message, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log<T>(sender, message, data.Concat(simData).ToArray());
        }

        #endregion
        #region with exception

        /// <summary>
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        public static void Log(Exception exception, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log(exception, data.Concat(simData).ToArray());
        }

        /// <summary>
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        public static void Log(object sender, Exception exception, IModel model, params Expression<Func<object, object>>[] data)
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
        public static void Log<T>(Exception exception, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log<T>(exception, data.Concat(simData).ToArray());
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
        public static void Log<T>(object sender, Exception exception, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log<T>(sender, exception, data.Concat(simData).ToArray());
        }

        #endregion
        #region with exception AND message

        /// <summary>
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        public static void Log(string message, Exception exception, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log(message, exception, data.Concat(simData).ToArray());
        }

        /// <summary>
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        public static void Log(object sender, string message, Exception exception, IModel model, params Expression<Func<object, object>>[] data)
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
        public static void Log<T>(string message, Exception exception, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log<T>(message, exception, data.Concat(simData).ToArray());
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
        public static void Log<T>(object sender, string message, Exception exception, IModel model, params Expression<Func<object, object>>[] data)
        {
            DateTime currentSimTime;
            if (model.CurrentTime >= DateTime.MaxValue.ToDouble()) currentSimTime = DateTime.MaxValue;
            else currentSimTime = model.CurrentTime.ToDateTime();

            Expression<Func<object, object>>[] simData = { Model => model, TimeStamp => currentSimTime };
            Logging.Logger.Log<T>(sender, message, exception, data.Concat(simData).ToArray());
        }

        #endregion
    }
}
