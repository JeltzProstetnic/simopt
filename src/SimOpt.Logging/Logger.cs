using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace SimOpt.Logging
{
    /// <summary>
    /// this class manages the logging
    /// </summary>
    public static class Logger
    {
        #region cvar

        private static bool autoDispatch;
        private static bool isDispatcherRunning;

        private static Thread dispatcherThread;
        private static AutoResetEvent dataAvailableEvent;
        private static AutoResetEvent dispatcherIdleEvent;
        
        private static volatile bool requestClear;
        private static volatile bool dispatcherEnabled;
        private static volatile bool requestDispatcherFinish;

        internal static ConcurrentDictionary<ILogger, ILoggerSettings> loggers;
        internal static ConcurrentQueue<Dictionary<string, object>> messageQueue;

        public readonly static string Sender = "Sender";
        public readonly static string Message = "Message";
        public readonly static string Severity = "Severity";
        public readonly static string TimeStamp = "TimeStamp";
        public readonly static string ExceptionS = "Exception";
        public readonly static string MessageClass = "MessageClass";
        public readonly static string MessageClassName = "MessageClassName";

        #endregion
        #region prop

        /// <summary>
        /// If this is set to true (default is true) all message 
        /// class types should have a severity float defined in 
        /// MessageClassSeverity otherwise their Severity
        /// will be set to zero.
        /// </summary>
        public static bool UseMessageClassAsSeverity { get; set; }

        /// <summary>
        /// if set to true in case of a log call with message AND exception 
        /// the message will be set to message + Exception.Message
        /// </summary>
        public static bool ConcatExceptionMessage { get; set; }

        /// <summary>
        /// a string to separate the given message from the exception message.
        /// </summary>
        public static string ConcatExceptionMessageSeparator { get; set; }

        /// <summary>
        /// map message classes to a severity number 
        /// if they are to be used as such.
        /// </summary>
        public static Dictionary<Type, float> MessageClassSeverity { get; set; }

        /// <summary>
        /// the message class to use if none is defined
        /// </summary>
        public static Type DefaultMessageClass { get; set; }
        
        /// <summary>
        /// the message class to use on exception messages if none is defined
        /// </summary>
        public static Type DefaultExceptionClass { get; set; }

        /// <summary>
        /// the loggers currently associated with this logger framework
        /// </summary>
        public static IEnumerable<ILogger> Loggers { get { return loggers.Keys; } }

        /// <summary>
        /// if this is set to true, the logging will occure instantly, 
        /// meaning that no threading will be used to dispatch messages.
        /// this makes your code wait until all loggers have consumed
        /// the message. if this is false, threading will be used to
        /// dispatch the messages concurrently.
        /// </summary>
        public static bool AutoDispatch
        {
            get { return autoDispatch; }
            set
            {
                if (value == autoDispatch) return;
                if (value && isDispatcherRunning) StopDispatcherThread();
                else if(!value && !isDispatcherRunning) StartDispatcherThread();
                autoDispatch = value;
            }
        }

        #endregion
        #region ctor

        static Logger()
        {
            dispatcherEnabled = true;

            ConcatExceptionMessage = true;
            ConcatExceptionMessageSeparator = " ";
            DefaultMessageClass = typeof(INFO);
            DefaultExceptionClass = typeof(ERROR);
            UseMessageClassAsSeverity = true;

            MessageClassSeverity = new Dictionary<Type, float>();
            MessageClassSeverity[typeof(INFO)] = 1f;
            MessageClassSeverity[typeof(WARN)] = 2f;
            MessageClassSeverity[typeof(ERROR)] = 3f;
            MessageClassSeverity[typeof(FATAL)] = 4f;
            MessageClassSeverity[typeof(STATUS)] = 0f;

            loggers = new ConcurrentDictionary<ILogger, ILoggerSettings>();
            messageQueue = new ConcurrentQueue<Dictionary<string, object>>();
        }

        #endregion
        #region impl

        internal static void Enqueue(Dictionary<string, object> data)
        {
            if (autoDispatch)
            {
                foreach (ILogger logger in loggers.Keys)
                {
                    if (loggers[logger].ProcessFilters(data))
                        logger.Log((DateTime)data[TimeStamp], data[Sender], (Type)data[MessageClass], (string)data[Message], data);
                }
            }
            else
            {
                if (!isDispatcherRunning) StartDispatcherThread();
                Logger.messageQueue.Enqueue(data);
                dataAvailableEvent.Set();
            }
        }

        #region dispatcher thread

        /// <summary>
        /// holds the current thread until the message queue is empty
        /// </summary>
        public static void Dispatch() 
        {
            if(isDispatcherRunning) dispatcherIdleEvent.WaitOne();
        }

        /// <summary>
        /// clears all messages currently in the queue, without logging them.
        /// </summary>
        public static void ClearMessageQueue() 
        {
            if (isDispatcherRunning)
            {
                requestClear = true;
                dataAvailableEvent.Set();
            }
        }

        /// <summary>
        /// Shutdown the dispatcher thread.
        /// </summary>
        /// <param name="immediately"></param>
        public static void Shutdown(bool immediately = true) 
        {
            if (!immediately) StopDispatcherThread();
            else if (isDispatcherRunning)
            {
                dispatcherEnabled = false;
                dataAvailableEvent.Set();
            }
            foreach (ILogger logger in loggers.Keys)
                logger.ShutdownLogger();
        }

        private static void StopDispatcherThread()
        {
            if (isDispatcherRunning)
            {
                requestDispatcherFinish = true;
                dataAvailableEvent.Set();
                dispatcherThread.Join();
                isDispatcherRunning = false;
            }
        }

        private static void StartDispatcherThread()
        {
            if (isDispatcherRunning) return;
            dataAvailableEvent = new AutoResetEvent(false);
            dispatcherIdleEvent = new AutoResetEvent(false);
            dispatcherThread = new Thread(DispatcherThread);
            dispatcherThread.IsBackground = true;
            dispatcherThread.Start();
            isDispatcherRunning = true;
        }

        private static void DispatcherThread()
        {
            bool dataAvailable = false;
            Dictionary<string, object> data;

            while (dispatcherEnabled)
            {
                dataAvailable = messageQueue.TryDequeue(out data);
                
                if (dataAvailable && loggers.Count > 0)
                {
                    foreach (ILogger logger in loggers.Keys)
                    {
                        if (loggers[logger].ProcessFilters(data))
                            logger.Log((DateTime)data[TimeStamp], data[Sender], (Type)data[MessageClass], (string)data[Message], data);
                        if (requestClear) break;
                    }
                }
                else
                {
                    dispatcherIdleEvent.Set();
                    dataAvailableEvent.WaitOne();
                    if (requestDispatcherFinish) break;
                }

                if (requestClear)
                {
                    messageQueue = new ConcurrentQueue<Dictionary<string, object>>();
                    requestClear = false;
                }
            }
        }

        #endregion
        #region explicit logging

        #region normal

        /// <summary>
        /// Enqueue or log a message.
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="data">further data coded as Key => Value expressions
        /// example: <code>Log("Hello World!", Weather => "Nice");</code></param>
        public static void Log(string message, params Expression<Func<object, object>>[] data)
        {
            Dictionary<string, object> tmp = new Dictionary<string, object>();

            tmp[Sender] = "UndefinedSender";
            tmp[Message] = message;
            tmp[TimeStamp] = DateTime.Now;
            tmp[MessageClass] = DefaultMessageClass;
            tmp[MessageClassName] = DefaultMessageClass.Name;

            if (UseMessageClassAsSeverity && MessageClassSeverity.ContainsKey(DefaultMessageClass))
                tmp[Severity] = MessageClassSeverity[DefaultMessageClass];
            else
                tmp[Severity] = 0f;

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
        /// <param name="data">further data coded as Key => Value expressions
        /// example: <code>Log("Hello World!", Weather => "Nice");</code></param>
        public static void Log(object sender, string message, params Expression<Func<object, object>>[] data)
        {
            Dictionary<string, object> tmp = new Dictionary<string, object>();
            
            tmp[Sender] = sender;
            tmp[Message] = message;
            tmp[TimeStamp] = DateTime.Now;
            tmp[MessageClass] = DefaultMessageClass;
            tmp[MessageClassName] = DefaultMessageClass.Name;

            if (UseMessageClassAsSeverity && MessageClassSeverity.ContainsKey(DefaultMessageClass))
                tmp[Severity] = MessageClassSeverity[DefaultMessageClass];
            else
                tmp[Severity] = 0f;

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
        /// <param name="message">The message to log</param>
        /// <param name="data">further data coded as Key => Value expressions
        /// example: <code>Log("Hello World!", Weather => "Nice");</code></param>
        public static void Log<T>(string message, params Expression<Func<object, object>>[] data)
        {
            Dictionary<string, object> tmp = new Dictionary<string, object>();

            tmp[Sender] = "UndefinedSender";
            tmp[Message] = message;
            tmp[TimeStamp] = DateTime.Now;
            tmp[MessageClass] = typeof(T);
            tmp[MessageClassName] = typeof(T).Name;

            if (UseMessageClassAsSeverity && MessageClassSeverity.ContainsKey(typeof(T)))
                tmp[Severity] = MessageClassSeverity[typeof(T)];
            else
                tmp[Severity] = 0f;

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
        /// <param name="data">
        /// further data coded as Key => Value expressions
        /// example: <code>Log("Hello World!", Weather => "Nice");</code>
        /// </param>
        public static void Log<T>(object sender, string message, params Expression<Func<object, object>>[] data)
        {
            Dictionary<string, object> tmp = new Dictionary<string, object>();
            
            tmp[Sender] = sender;
            tmp[Message] = message;
            tmp[TimeStamp] = DateTime.Now;
            tmp[MessageClass] = typeof(T);
            tmp[MessageClassName] = typeof(T).Name;
            
            if (UseMessageClassAsSeverity && MessageClassSeverity.ContainsKey(typeof(T)))
                tmp[Severity] = MessageClassSeverity[typeof(T)];
            else
                tmp[Severity] = 0f;

            foreach (Expression<Func<object, object>> e in data)
            {
                tmp[e.Parameters[0].Name] = e.Compile().Invoke(null);
            }

            Logger.Enqueue(tmp);
        }

        #endregion
        #region with exception

        /// <summary>
        /// Enqueue or log a message.
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="exception">The exception related to this message</param>
        /// <param name="data">further data coded as Key => Value expressions
        /// example: <code>Log("Hello World!", Weather => "Nice");</code></param>
        public static void Log(Exception exception, params Expression<Func<object, object>>[] data)
        {
            Dictionary<string, object> tmp = new Dictionary<string, object>();

            tmp[Sender] = "UndefinedSender";
            tmp[Message] = exception.Message;
            tmp[TimeStamp] = DateTime.Now;
            tmp[ExceptionS] = exception;
            tmp[MessageClass] = DefaultExceptionClass;
            tmp[MessageClassName] = DefaultExceptionClass.Name;

            if (UseMessageClassAsSeverity && MessageClassSeverity.ContainsKey(DefaultExceptionClass))
                tmp[Severity] = MessageClassSeverity[DefaultExceptionClass];
            else
                tmp[Severity] = 0f;

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
        public static void Log(object sender, Exception exception, params Expression<Func<object, object>>[] data)
        {
            Dictionary<string, object> tmp = new Dictionary<string, object>();

            tmp[Sender] = sender;
            tmp[Message] = exception.Message;
            tmp[TimeStamp] = DateTime.Now;
            tmp[ExceptionS] = exception;
            tmp[MessageClass] = DefaultExceptionClass;
            tmp[MessageClassName] = DefaultExceptionClass.Name;

            if (UseMessageClassAsSeverity && MessageClassSeverity.ContainsKey(DefaultExceptionClass))
                tmp[Severity] = MessageClassSeverity[DefaultExceptionClass];
            else
                tmp[Severity] = 0f;

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
        /// <param name="exception">The exception related to this message</param>
        /// <param name="data">further data coded as Key => Value expressions
        /// example: <code>Log("Hello World!", Weather => "Nice");</code></param>
        public static void Log<T>(Exception exception, params Expression<Func<object, object>>[] data)
        {
            Dictionary<string, object> tmp = new Dictionary<string, object>();

            tmp[Sender] = "UndefinedSender";
            tmp[Message] = exception.Message;
            tmp[TimeStamp] = DateTime.Now;
            tmp[ExceptionS] = exception;
            tmp[MessageClass] = typeof(T);
            tmp[MessageClassName] = typeof(T).Name;

            if (UseMessageClassAsSeverity && MessageClassSeverity.ContainsKey(typeof(T)))
                tmp[Severity] = MessageClassSeverity[typeof(T)];
            else
                tmp[Severity] = 0f;

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
        public static void Log<T>(object sender, Exception exception, params Expression<Func<object, object>>[] data)
        {
            Dictionary<string, object> tmp = new Dictionary<string, object>();

            tmp[Sender] = sender;
            tmp[Message] = exception.Message;
            tmp[TimeStamp] = DateTime.Now;
            tmp[ExceptionS] = exception;
            tmp[MessageClass] = typeof(T);
            tmp[MessageClassName] = typeof(T).Name;

            if (UseMessageClassAsSeverity && MessageClassSeverity.ContainsKey(typeof(T)))
                tmp[Severity] = MessageClassSeverity[typeof(T)];
            else
                tmp[Severity] = 0f;

            foreach (Expression<Func<object, object>> e in data)
            {
                tmp[e.Parameters[0].Name] = e.Compile().Invoke(null);
            }

            Logger.Enqueue(tmp);
        }

        #endregion
        #region with exception AND message

        /// <summary>
        /// Enqueue or log a message.
        /// CAUTION: if "Sender" or "Message" appears as key in data, 
        /// the sender or message will be replaced by these!
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="exception">The exception related to this message</param>
        /// <param name="data">further data coded as Key => Value expressions
        /// example: <code>Log("Hello World!", Weather => "Nice");</code></param>
        public static void Log(string message, Exception exception, params Expression<Func<object, object>>[] data)
        {
            string msg;
            if (Logger.ConcatExceptionMessage)
                msg = message + ConcatExceptionMessageSeparator + exception.Message;
            else
                msg = message;
            Dictionary<string, object> tmp = new Dictionary<string, object>();

            tmp[Sender] = "UndefinedSender";
            tmp[Message] = msg;
            tmp[TimeStamp] = DateTime.Now;
            tmp[ExceptionS] = exception;
            tmp[MessageClass] = DefaultExceptionClass;
            tmp[MessageClassName] = DefaultExceptionClass.Name;

            if (UseMessageClassAsSeverity && MessageClassSeverity.ContainsKey(DefaultExceptionClass))
                tmp[Severity] = MessageClassSeverity[DefaultExceptionClass];
            else
                tmp[Severity] = 0f;

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
        public static void Log(object sender, string message, Exception exception, params Expression<Func<object, object>>[] data)
        {
            string msg;
            if (Logger.ConcatExceptionMessage)
                msg = message + ConcatExceptionMessageSeparator + exception.Message;
            else
                msg = message;
            Dictionary<string, object> tmp = new Dictionary<string, object>();

            tmp[Sender] = sender;
            tmp[Message] = msg;
            tmp[TimeStamp] = DateTime.Now;
            tmp[ExceptionS] = exception;
            tmp[MessageClass] = DefaultExceptionClass;
            tmp[MessageClassName] = DefaultExceptionClass.Name;

            if (UseMessageClassAsSeverity && MessageClassSeverity.ContainsKey(DefaultExceptionClass))
                tmp[Severity] = MessageClassSeverity[DefaultExceptionClass];
            else
                tmp[Severity] = 0f;

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
        /// <param name="message">The message to log</param>
        /// <param name="exception">The exception related to this message</param>
        /// <param name="data">further data coded as Key => Value expressions
        /// example: <code>Log("Hello World!", Weather => "Nice");</code></param>
        public static void Log<T>(string message, Exception exception, params Expression<Func<object, object>>[] data)
        {
            string msg;
            if (Logger.ConcatExceptionMessage)
                msg = message + ConcatExceptionMessageSeparator + exception.Message;
            else
                msg = message;
            Dictionary<string, object> tmp = new Dictionary<string, object>();

            tmp[Sender] = "UndefinedSender";
            tmp[Message] = msg;
            tmp[TimeStamp] = DateTime.Now;
            tmp[ExceptionS] = exception;
            tmp[MessageClass] = typeof(T);
            tmp[MessageClassName] = typeof(T).Name;

            if (UseMessageClassAsSeverity && MessageClassSeverity.ContainsKey(typeof(T)))
                tmp[Severity] = MessageClassSeverity[typeof(T)];
            else
                tmp[Severity] = 0f;

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
        public static void Log<T>(object sender, string message, Exception exception, params Expression<Func<object, object>>[] data)
        {
            string msg;
            if (Logger.ConcatExceptionMessage)
                msg = message + ConcatExceptionMessageSeparator + exception.Message;
            else
                msg = message;
            Dictionary<string, object> tmp = new Dictionary<string, object>();

            tmp[Sender] = sender;
            tmp[Message] = msg;
            tmp[TimeStamp] = DateTime.Now;
            tmp[ExceptionS] = exception;
            tmp[MessageClass] = typeof(T);
            tmp[MessageClassName] = typeof(T).Name;

            if (UseMessageClassAsSeverity && MessageClassSeverity.ContainsKey(typeof(T)))
                tmp[Severity] = MessageClassSeverity[typeof(T)];
            else
                tmp[Severity] = 0f;

            foreach (Expression<Func<object, object>> e in data)
            {
                tmp[e.Parameters[0].Name] = e.Compile().Invoke(null);
            }

            Logger.Enqueue(tmp);
        }

        #endregion

        #endregion
        #region add or remove loggers

        /// <summary>
        /// add a logger to be fed with messages
        /// </summary>
        /// <param name="logger">the log message consumer</param>
        /// <param name="settings">logger settings</param>
        public static void Add(ILogger logger, ILoggerSettings settings = null)
        {
            if (settings == null) loggers[logger] = new DefaultLoggerSettings();
            else loggers[logger] = settings;
        }

        /// <summary>
        /// add a logger to be fed with messages using a whitelist to
        /// decide which messages to send to the logger and which to filter.
        /// </summary>
        /// <param name="logger">the log message consumer</param>
        /// <param name="whiteList">a number of message classes which will be fed to the logger</param>
        public static void Add(ILogger logger, params Type[] whiteList)
        {
            loggers[logger] = new DefaultLoggerSettings(whiteList);
        }

        /// <summary>
        /// add a logger to be fed with messages using a 
        /// minimum severity to filter messages
        /// </summary>
        /// <param name="logger">the log message consumer</param>
        /// <param name="minimumSeverity">a minimum severity below which messages are hidden from the logger</param>
        public static void Add(ILogger logger, int minimumSeverity)
        {
            loggers[logger] = new DefaultLoggerSettings(minimumSeverity);
        }

        /// <summary>
        /// add a logger to be fed with messages
        /// </summary>
        /// <typeparam name="T">the message class to be consumed by the logger (filter)</typeparam>
        /// <param name="logger">the log message consumer</param>
        public static void Add<T>(ILogger logger)
        {
            loggers[logger] = new DefaultLoggerSettings(typeof(T));
        }

        /// <summary>
        /// add a logger to be fed with messages
        /// </summary>
        /// <typeparam name="T1">a message class to be consumed by the logger (filter)</typeparam>
        /// <typeparam name="T2">a message class to be consumed by the logger (filter)</typeparam>
        /// <param name="logger">the log message consumer</param>
        public static void Add<T1, T2>(ILogger logger)
        {
            loggers[logger] = new DefaultLoggerSettings(typeof(T1), typeof(T2));
        }

        /// <summary>
        /// add a logger to be fed with messages
        /// </summary>
        /// <typeparam name="T1">a message class to be consumed by the logger (filter)</typeparam>
        /// <typeparam name="T2">a message class to be consumed by the logger (filter)</typeparam>
        /// <typeparam name="T3">a message class to be consumed by the logger (filter)</typeparam>
        /// <param name="logger">the log message consumer</param>
        public static void Add<T1, T2, T3>(ILogger logger)
        {
            loggers[logger] = new DefaultLoggerSettings(typeof(T1), typeof(T2), typeof(T3));
        }

        /// <summary>
        /// add a logger to be fed with messages
        /// </summary>
        /// <typeparam name="T1">a message class to be consumed by the logger (filter)</typeparam>
        /// <typeparam name="T2">a message class to be consumed by the logger (filter)</typeparam>
        /// <typeparam name="T3">a message class to be consumed by the logger (filter)</typeparam>
        /// <typeparam name="T4">a message class to be consumed by the logger (filter)</typeparam>
        /// <param name="logger">the log message consumer</param>
        public static void Add<T1, T2, T3, T4>(ILogger logger)
        {
            loggers[logger] = new DefaultLoggerSettings(typeof(T1), typeof(T2), typeof(T3), typeof(T4));
        }

        /// <summary>
        /// add a logger to be fed with messages
        /// </summary>
        /// <param name="logger">the log message consumer</param>
        /// <param name="filter">
        /// a filter function. the filter function will recieve 
        /// a dictionary containing all the data which was provided
        /// in the log message. Example: <code>Logger.Add(SomeLogger, data => ((string)data["Message"]).Contains("something"));</code>
        /// </param>
        public static void Add(ILogger logger, Func<Dictionary<string, object>, bool> filter)
        {
            loggers[logger] = new DefaultLoggerSettings();
        }

        /// <summary>
        /// returns false if the logger was not found in the internal dictionary.
        /// </summary>
        /// <param name="logger">the logger to be removed</param>
        /// <returns>success flag</returns>
        public static bool Remove(ILogger logger)
        {
            ILoggerSettings nothing;
            return loggers.TryRemove(logger, out nothing);
        }

        #endregion
        #region register message classes

        /// <summary>
        /// use this to register message classes with a given severity
        /// so they can be filtered
        /// </summary>
        /// <param name="messageClass"></param>
        /// <param name="severity"></param>
        public static void RegisterMessageClass(Type messageClass, float severity = 0)
        {
            MessageClassSeverity[messageClass] = severity;
        }

        /// <summary>
        /// use this to register message classes with a given severity
        /// so they can be filtered
        /// </summary>
        /// <param name="messageClass"></param>
        /// <param name="severity"></param>
        public static void RegisterMessageClass<T>(float severity = 0)
        {
            MessageClassSeverity[typeof(T)] = severity;
        }

        #endregion

        #endregion
    }
}
