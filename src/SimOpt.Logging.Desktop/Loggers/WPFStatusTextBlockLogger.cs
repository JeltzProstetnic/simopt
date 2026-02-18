using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SimOpt.Logging.Loggers
{
    public class WPFStatusTextBlockLogger : ILogger
    {
        private bool enabled = true;
        private TextBlock statusTextBlock;

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

        public WPFStatusTextBlockLogger(TextBlock statusTextBlock) 
        {
            this.statusTextBlock = statusTextBlock;
        }

        public void Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
        {
            if (!enabled) return;

            Action update = delegate
            {
                statusTextBlock.Text = message;
            };

            try
            {
                statusTextBlock.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, update);
            }
            catch (InvalidOperationException ex) // the window was destroyed
            {
#if DEBUG
                Console.WriteLine("The WPFStatusTextBlockLogger was unable to write to the TextBlock: " + ex.Message);
#endif
            }
        }

        public void ShutdownLogger()
        {
            enabled = false;
        }
    }
}
