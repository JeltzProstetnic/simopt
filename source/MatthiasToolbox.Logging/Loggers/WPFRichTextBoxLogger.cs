using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace MatthiasToolbox.Logging.Loggers
{
    public class WPFRichTextBoxLogger : ILogger
    {
        private bool enabled = true;
        private RichTextBox textBox;

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

        public WPFRichTextBoxLogger(RichTextBox textBox, bool printStackTrace = true) 
        {
            this.textBox = textBox;
            PrintStackTrace = printStackTrace;
        }

        public virtual void Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
        {
            if (!enabled) return;

            string msg;
            string s = sender as string;
            if (string.IsNullOrEmpty(s))
            {
                try { s = sender.ToString(); }
                catch { s = "Unknown Sender"; }
            }

            msg = timeStamp.ToString() + " - " + messageClass.Name + " @ " + s + " - " + message + "\r";

            if (PrintStackTrace && data.ContainsKey("Exception"))
                msg += ((Exception)data["Exception"]).StackTrace + "\n";

            Action update = delegate
            {
                textBox.AppendText(msg);
                textBox.ScrollToEnd();
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
            OnShutDown();
        }

        public virtual void OnShutDown() { }
    }
}
