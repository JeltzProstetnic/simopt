using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using MatthiasToolbox.Money.Data;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Logging.Loggers;
using System.IO;
using System.Threading;

namespace MatthiasToolbox.Money
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region cvar

        private FileInfo logFile;
        private bool resetDB;
        private bool splashEnabled;
        private List<string> commandLineArgs;
        private Splash splashScreen;

        #endregion
        #region main

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // #############################
            // ##  Non-Persistable Flags  ##
            // #############################
            Global.GodMode = File.Exists("me.god");
            splashEnabled = !File.Exists("splash.off");

            // #####################
            // ##  Setup Logging  ##
            // #####################
            if (!SetupLogging()) return;

            // ####################################
            // ##  Import Commandline Arguments  ##
            // ####################################
            if (!ParseCommandline(e)) return;

            // ##########################
            // ##  Show Splash Screen  ##
            // ##########################
            DateTime startedSplash = DateTime.Now;
            if (splashEnabled)
            {
                splashScreen = new Splash();
                splashScreen.Show();
            }

            // ###########################
            // ##  Initialize Database  ##
            // ###########################
            if (!Database.Initialize("FinanceManager.sdf", resetDB))
            {
                this.Log<FATAL>("OMG! A database error occured!");
                MessageBox.Show("There was a problem initializing the database. " + 
                   "See logfile for further details.", "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
                try { Logger.Shutdown(true); } catch { }
                return;
            }

            // ############
            // ##  MAIN  ##
            // ############
            this.Log<INFO>("Starting GUI.");
            MainWindow mainWindow = new MainWindow();

            if (splashEnabled)
            {
                // show splash for at least 1/2 sec.
                double passedMS = DateTime.Now.Subtract(startedSplash).TotalMilliseconds;
                if (passedMS < 500) Thread.Sleep(500 - (int)passedMS);

                // close splash screen
                // splashScreen.Close(); // prevents mainWindow.ShowDialog for some reason
                splashScreen.Hide();
            }

            // open main dialog
            if (!resetDB)
            {
                mainWindow.ShowDialog();
            }
            else
            {
                mainWindow.Show();  // god knows why this is needed...
                MainWindow.Close(); // but if I don't do it the app hangs after the message box.
                MessageBox.Show("Database cleared.", "DB Reset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            if (splashEnabled) splashScreen.Close();

            // kill logging thread
            Logger.Shutdown(true);
        }

        #endregion
        #region impl

        /// <summary>
        /// initialize logger with a PlainTextFile logger
        /// and a ConsoleLogger (only in DEBUG mode)
        /// </summary>
        /// <returns>success flag</returns>
        private bool SetupLogging()
        {
            try
            {
                // backup and delete old logfiles
                logFile = new FileInfo(Global.LogFileName);
                if (logFile.Exists && logFile.Length > Global.MaxLogSize) 
                    File.Copy(logFile.Name, Global.LogFileBackupName, true);
                
                // create and start log file
                Logger.Add(new PlainTextFileLogger(logFile));
#if DEBUG
                Logger.Add(new ConsoleLogger());
                Global.GodMode = true;
#endif
                this.Log<INFO>("FinanceManager started.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to initialize logging. Make sure the application has read and write access to its current path.\n" + ex.Message, "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
                try { Logger.Shutdown(true); }
                catch { }
                return false;
            }
            return true;
        }

        /// <summary>
        /// parse and interpret commandline args
        /// </summary>
        /// <param name="e">the StartupEventArgs provided by Application_Startup</param>
        /// <returns>success flag</returns>
        private bool ParseCommandline(StartupEventArgs e)
        {
            try
            {
                List<string> commandLineArgsOriginal = e.Args.ToList<string>();
                commandLineArgs = new List<string>();
                foreach (string arg in commandLineArgsOriginal) commandLineArgs.Add(arg.ToLower().Replace("-", "").Replace("/", "").Trim());
                resetDB = commandLineArgs.Contains("resetdb");
#if DEBUG
                resetDB = true;
#endif
                if (resetDB)
                {
                    this.Log<WARN>("The resetdb commandline argument was received. If the database exists it will be deleted.");
                    if (MessageBox.Show("Do you really want to delete all data?", "Reset Database", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                        != MessageBoxResult.Yes) 
                        resetDB = false;
                }
            }
            catch (Exception ex)
            {
                this.Log<FATAL>(ex);
                MessageBox.Show("There was a problem parsing the commandline arguments. See logfile for further details.", 
                    "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
                try { Logger.Shutdown(true); }
                catch { }
                return false;
            }
            return true;
        }

        #endregion
    }
}
