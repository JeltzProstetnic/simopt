using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MatthiasToolbox.Logging.Loggers
{
    public class StatusBarLogger : ILogger
    {
        private bool enabled = true;
        private Control statusBar;
        private ToolStripStatusLabel label;
        private bool isStrip = false;

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

        public StatusBarLogger(StatusBar statusBar) 
        {
            this.statusBar = statusBar;
        }

        public StatusBarLogger(ToolStripStatusLabel toolStripStatusLabel)
        {
            this.label = toolStripStatusLabel;
            isStrip = true;
        }

        public void Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
        {
            if (!enabled) return;

            MethodInvoker update = delegate
            {
                if (isStrip) label.Text = message;
                else statusBar.Text = message;
                Application.DoEvents();
            };

            try
            {
                if (isStrip) label.GetCurrentParent().Invoke(update);
                else statusBar.Invoke(update);
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
