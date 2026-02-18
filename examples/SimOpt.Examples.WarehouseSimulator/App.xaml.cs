using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.IO;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Logging.Loggers;
using Vr.WarehouseSimulator.Data;
using System.Data.OracleClient;
// using MatthiasToolbox.Simulation;

#pragma warning disable 0618 // OracleClient is obsolet - switch to devart dotConnect before migrating to .NET 5.0

namespace Vr.WarehouseSimulator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region cvar

        // flags
        private bool testMode;
        private bool splashEnabled;

        // files
        private FileInfo logFile;
        private FileInfo simLogFile;
        
        // arguments
        private List<string> commandLineArgs;
        
        // connections
        private string layoutConnectionString;
        private string orderConnectionString;
        private string processConnectionString;

        // windows
        // private Splash splashScreen;
        // DateTime startedSplash;
        private Window window;

        #endregion
        #region main

        /// <summary>
        /// main
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Non-Persistable Flags
            Global.GodMode = File.Exists("me.god");
            splashEnabled = !File.Exists("splash.off");

            // Setup Logging
            if (!SetupLogging()) return;

            // Import Commandline Arguments
            if (!ParseCommandline(e)) return;

            // Show Splash Screen
            if (splashEnabled) StartSplash();
            
            // Initialize Database
            if (!InitializeDatabase()) return;

            // MAIN
            this.Log<INFO>("Starting GUI.");
            if (!testMode) window = new MainWindow();
            else window = new TestWindow();

            // Hide Splash Screen
            if (splashEnabled) EndSplashScreen();

            // open main dialog
            window.ShowDialog();
            
            // finally close splash screen
            // if (splashEnabled) splashScreen.Close();

            // kill db connection
            Global.LayoutDatabase.Connection.Close();

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
                // create file infos
                logFile = new FileInfo(Global.LogFileName);
                simLogFile = new FileInfo(Global.SimLogFileName);

                // backup and delete old logfiles
                if (logFile.Exists && logFile.Length > Global.MaxLogSize) 
                    File.Copy(logFile.Name, Global.LogFileBackupName, true);
                if (simLogFile.Exists && simLogFile.Length > Global.MaxLogSize)
                    File.Copy(simLogFile.Name, Global.SimLogFileBackupName, true);

                // create and start application log file
                Logger.Add(new PlainTextFileLogger(logFile));
                
                // create and start simulation log file // TODO: FIXME: use simulationLogger
                MatthiasToolbox.Simulation.Simulator.RegisterSimulationLogger(new PlainTextFileLogger(simLogFile));
#if DEBUG
                Logger.Add(new ConsoleLogger());
                Global.GodMode = true;
#endif
                this.Log<INFO>("WarehouseSimulator started.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to initialize logging. Make sure the application has read and write access to its current path.\n" + ex.Message, "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
                try { Logger.Shutdown(true); } 
                catch { /* CANNOT RECOVER */ }
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
                foreach (string arg in commandLineArgsOriginal) 
                    commandLineArgs.Add(arg.ToLower().Replace("-", "").Replace("/", "").Trim());
                testMode = commandLineArgs.Contains("test");
#if DEBUG
                testMode = true;
#endif
                if (testMode) this.Log<INFO>("WarehouseSimulator started in test mode.");
            }
            catch (Exception ex)
            {
                this.Log<FATAL>(ex);
                MessageBox.Show("There was a problem parsing the commandline arguments. See logfile for further details.", 
                    "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
                try { Logger.Shutdown(true); }
                catch { /* CANNOT RECOVER */ }
                return false;
            }
            return true;
        }

        /// <summary>
        /// initialize database connections
        /// </summary>
        /// <returns>success flag</returns>
        private bool InitializeDatabase() 
        {
            if (!GetConnectionStrings()) return false;
            if (!OpenDBs()) return false;
            if (!InitDBs()) return false;

            return true;
        }

        /// <summary>
        /// display the splash screen
        /// </summary>
        private void StartSplash() 
        {
            // startedSplash = DateTime.Now;
            // splashScreen = new Splash();
            // splashScreen.Show();
        }

        /// <summary>
        /// hide the splash screen
        /// </summary>
        private void EndSplashScreen()
        {
            //// show splash for at least 1/2 sec.
            //double passedMS = DateTime.Now.Subtract(startedSplash).TotalMilliseconds;
            //if (passedMS < 500) Thread.Sleep(500 - (int)passedMS);

            //// close splash screen
            //// splashScreen.Close(); // prevents mainWindow.ShowDialog for some reason
            //splashScreen.Hide();
        }

        #endregion
        #region util

        private bool GetConnectionStrings()
        {
            try
            {
                FileInfo connectionFile = new FileInfo(Global.ConnectionStringFileName);
                StreamReader sr = connectionFile.OpenText();
                string content = sr.ReadToEnd();
                List<String> lines = content.Split('\n').ToList();

                layoutConnectionString = lines[0].Split('|')[1];
                orderConnectionString = lines[1].Split('|')[1];
                processConnectionString = lines[2].Split('|')[1];

                return true;
            }
            catch (Exception e)
            {
                this.Log<FATAL>(e);
                
                MessageBox.Show("There was a problem parsing the connection strings in <" + Global.ConnectionStringFileName +
                    ">. See logfile for further details.", "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                try { Logger.Shutdown(true); }
                catch { /* CANNOT RECOVER */ }
                
                return false;
            }
        }

        private bool OpenDBs()
        {
            try
            {
                // Global.LayoutDatabase = new LayoutDatabase(layoutConnectionString); // for use with devart dotConnect

                // workaround for OracleClient
                OracleConnection ocnLayout = new OracleConnection(layoutConnectionString);
                ocnLayout.Open();
                Global.LayoutDatabase = new LayoutDatabase(ocnLayout);

                OracleConnection ocnOrder = new OracleConnection(orderConnectionString);
                ocnOrder.Open();
                Global.OrderDatabase = new OrderDatabase(ocnOrder);

                OracleConnection ocnProcess = new OracleConnection(processConnectionString);
                ocnProcess.Open();
                Global.ProcessDatabase = new ProcessDatabase(ocnProcess);

                return true;
            }
            catch (Exception e)
            {
                this.Log<FATAL>("Error connecting to DB. Connection = " + layoutConnectionString + "\n", e);
                MessageBox.Show("There was a problem connecting to the database <" + layoutConnectionString +
                    ">. See logfile for further details.", "Connection error", MessageBoxButton.OK, MessageBoxImage.Error);
                try { Logger.Shutdown(true); }
                catch { /* CANNOT RECOVER */ }
                return false;
            }
        }

        private bool InitDBs()
        {
            if (!Global.LayoutDatabase.Initialize())
            {
                this.Log<FATAL>("OMG! Error initializing DB. Connection = " + layoutConnectionString);
                MessageBox.Show("There was a problem initializing the database <" + layoutConnectionString +
                    ">. See logfile for further details.", "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
                try { Logger.Shutdown(true); }
                catch { /* CANNOT RECOVER */ }
                return false;
            }
            
            if (!Global.OrderDatabase.Initialize())
            {
                this.Log<FATAL>("OMG! Error initializing DB. Connection = " + orderConnectionString);
                MessageBox.Show("There was a problem initializing the database <" + orderConnectionString +
                    ">. See logfile for further details.", "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
                try { Logger.Shutdown(true); }
                catch { /* CANNOT RECOVER */ }
                return false;
            }

            if (!Global.ProcessDatabase.Initialize())
            {
                this.Log<FATAL>("OMG! Error initializing DB. Connection = " + processConnectionString);
                MessageBox.Show("There was a problem initializing the database <" + processConnectionString +
                    ">. See logfile for further details.", "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
                try { Logger.Shutdown(true); }
                catch { /* CANNOT RECOVER */ }
                return false;
            }

            return true;
        }

        #endregion
    }
}
