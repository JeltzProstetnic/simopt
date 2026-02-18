using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace MatthiasToolbox.Logging.Loggers
{
    public class WPFTextBoxLogger : ILogger
    {
        private bool enabled = true;
        private TextBox textBox;

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

        public WPFTextBoxLogger(TextBox textBox) 
        {
            this.textBox = textBox;
        }

        public void Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
        {
            if (!enabled) return;

            string msg;

            string s = sender as string;
            if (string.IsNullOrEmpty(s))
            {
                try { s = sender.ToString(); }
                catch { s = "Unknown Sender"; }
            }

            msg = timeStamp.ToString() + " - " + messageClass.Name + " @ " + s + " - " + message + "\n";

            if (PrintStackTrace && data.ContainsKey("Exception"))
                msg += ((Exception)data["Exception"]).StackTrace + "\n";

            Action update = delegate
            {
                textBox.Text += msg;
                textBox.ScrollToLine(textBox.LineCount);
            };

            try
            {
                textBox.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, update);
            }
            catch (InvalidOperationException ex) // the window was destroyed
            {
#if DEBUG
                Console.WriteLine("The StatusBarLogger was unable to write to the statusBar: " + ex.Message);
#endif
            }
        }

        public void ShutdownLogger()
        {
            enabled = false;
        }
    }
}
