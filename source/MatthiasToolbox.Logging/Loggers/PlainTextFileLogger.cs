using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MatthiasToolbox.Logging.Loggers
{
    public class PlainTextFileLogger : ILogger
    {
        private bool enabled = true;
        private FileInfo file;
		private StreamWriter stream;

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

		/// <summary>
		/// redirect console output to the given file
		/// </summary>
		/// <param name="textBox"></param>
   		public PlainTextFileLogger(FileInfo file) 
   		{ 
      		this.file = file;
      		stream = file.AppendText();
   		}

        // does not seem to occur in a static class
        //~PlainTextFileLogger()
        //{
        //    stream.Flush();
        //    stream.Close();
        //}

        public void Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
        {
            if (!enabled) return;

            string s = sender as string;
            if (string.IsNullOrEmpty(s))
            {
                try { s = sender.ToString(); }
                catch { s = "Unknown Sender"; }
            }

            stream.WriteLine(timeStamp.ToString() + " - " + messageClass.Name + " @ " + s + " - " + message);

            if (PrintStackTrace && data.ContainsKey("Exception"))
                stream.Write(((Exception)data["Exception"]).StackTrace + "\n");

            stream.Flush();
        }

        public void ShutdownLogger() 
        {
            enabled = false;
            stream.Close();
        }

        
    }
}
