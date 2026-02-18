using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimOpt.Logging.Loggers
{
    public class TextBoxLogger : ILogger
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

        public TextBoxLogger(TextBox textBox) 
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

            MethodInvoker update = delegate
            {
                textBox.Text += msg;
                textBox.Select(textBox.Text.Length - 1, 0);
                textBox.ScrollToCaret();
                Application.DoEvents();
            };

            try
            {
                textBox.Invoke(update);
            }
            catch (InvalidOperationException ex) // the window was destroyed
            {
#if DEBUG
                Console.WriteLine("The TextBoxLogger was unable to write to the TextBox: " + ex.Message);
#endif
            }
        }

        public void ShutdownLogger()
        {
            enabled = false;
        }
    }
}
