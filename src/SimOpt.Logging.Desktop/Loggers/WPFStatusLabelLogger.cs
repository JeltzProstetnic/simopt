using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SimOpt.Logging.Loggers
{
    public class WPFStatusLabelLogger : ILogger
    {
        private bool enabled = true;
        private Label statusLabel;

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

        public WPFStatusLabelLogger(Label statusLabel) 
        {
            this.statusLabel = statusLabel;
        }

        public void Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
        {
            if (!enabled) return;

            Action update = delegate
            {
                statusLabel.Content = message;
            };

            try
            {
                statusLabel.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, update);
            }
            catch (InvalidOperationException ex) // the window was destroyed
            {
#if DEBUG
                Console.WriteLine("The WPFStatusLabelLogger was unable to write to the Label: " + ex.Message);
#endif
            }
        }

        public void ShutdownLogger()
        {
            enabled = false;
        }
    }
}
