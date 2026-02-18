using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Logging.Loggers
{
    public class ConsoleLogger : ILogger
    {
        private bool enabled = true;

        public bool LoggingEnabled
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
            }
        }

        public bool PrintStackTrace { get; set; }

        public void Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
        {
            if (!enabled) return;

            string s = sender as string;
            if (string.IsNullOrEmpty(s))
            {
                try { s = sender.ToString(); }
                catch { s = "Unknown Sender"; }
            }

            Console.WriteLine(timeStamp.ToString() + " - " + messageClass.Name + " @ " + s + " - " + message);

            if (PrintStackTrace && data.ContainsKey("Exception"))
                Console.Write(((Exception)data["Exception"]).StackTrace + "\n");
        }

        public void ShutdownLogger()
        {
            enabled = false;
        }
    }
}
