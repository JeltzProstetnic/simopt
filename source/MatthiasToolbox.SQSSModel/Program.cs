using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Logging.Loggers;
using System.Threading;
using System.IO;
using System.Reflection;

namespace MatthiasToolbox.SQSSModel
{
    static class Program
    {
        private static readonly string me = "MatthiasToolbox.SQSSModel.Program";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string appPath = Assembly.GetExecutingAssembly().Location.Substring(0, Assembly.GetExecutingAssembly().Location.LastIndexOf('\\'));
            string logFile = appPath + "\\MatthiasToolbox.SQSSModel.log";

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            Logger.Add(new ConsoleLogger());
            Logger.Add(new PlainTextFileLogger(new FileInfo(logFile)));
            Logger.Log(me, "Starting up gui...");
            Logger.Dispatch();
            
            Application.Run(new Form1());
            
            Logger.Log("Shutting down.");
            Logger.Shutdown(false);
        }
    }
}
