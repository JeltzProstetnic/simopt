using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;

using MatthiasToolbox.Geography;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Logging.Loggers;
using MatthiasToolbox.SupplyChain.Database;
using MatthiasToolbox.Utilities;

namespace MatthiasToolbox.SupplyChain
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region cvar

        // flags
        private bool splashEnabled;

        // files
        private FileInfo logFile;

        // arguments
        private List<string> commandLineArgs;

        // connections
        private string geoConnectionString;
        private string modelConnectionString;
        private string userConnectionString;

        // windows
        // private Splash splashScreen;
        // DateTime startedSplash;
        private Window window;

        #endregion
        #region main

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Non-Persistable Flags
            Global.GodMode = File.Exists("me.god");
            Global.AdminMode = File.Exists("me.admin");
            splashEnabled = !File.Exists("splash.off");

            // Setup Logging
            if (!SetupLogging()) return;

            // Import Commandline Arguments
            if (!ParseCommandline(e)) return;

            // Initialize Database
            if (!InitializeDatabase()) return;

            // MAIN
            this.Log<INFO>("Starting GUI.");
            window = new MainWindow();

            // open main dialog
            window.ShowDialog();

            // kill db connections
            CloseConnections();
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

                // backup and delete old logfiles
                if (logFile.Exists && logFile.Length > Global.MaxLogSize)
                    File.Copy(logFile.Name, Global.LogFileBackupName, true);

                // create and start application log file
                Logger.Add(new PlainTextFileLogger(logFile));
#if DEBUG
                Logger.Add(new ConsoleLogger());
#endif
                this.Log<INFO>("Supply Chain Simulator started.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to initialize logging. Make sure the application has read and write access to its current path.\n" + ex.Message, "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Global.TestMode = commandLineArgs.Contains("test");
#if DEBUG
                Global.TestMode = true;
#endif
                if (Global.TestMode) this.Log<INFO>("Supply Chain Simulator running in test mode.");

//              string app = Environment.GetCommandLineArgs()[0];
//              int exePos = app.LastIndexOf("\\");
// 				 Global.ApplicationPath = app.Substring(0, exePos);
                Global.ApplicationPath = SystemTools.ExecutablePath();
            }
            catch (Exception ex)
            {
                this.Log<FATAL>(ex);
                MessageBox.Show("There was a problem parsing the commandline arguments. See logfile for further details.",
                    "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            Logger.Add(Global.UserDatabase);

            return true;
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

                geoConnectionString = lines[0].Split('|')[1].Trim();
                if (geoConnectionString.ToLower().EndsWith(".sdf")) // connection file
                {
                    geoConnectionString = Global.ApplicationPath + "\\Resources\\" + geoConnectionString;
                }

                modelConnectionString = lines[1].Split('|')[1].Trim();
                if (modelConnectionString.ToLower().EndsWith(".sdf")) // connection file
                {
                    modelConnectionString = Global.ApplicationPath + "\\" + modelConnectionString;
                }

                userConnectionString = lines[2].Split('|')[1].Trim();
                if (userConnectionString.ToLower().EndsWith(".sdf")) // connection file
                {
                    userConnectionString = Global.ApplicationPath + "\\" + userConnectionString;
                }

                return true;
            }
            catch (Exception e)
            {
                this.Log<FATAL>(e);
                MessageBox.Show("There was a problem parsing the connection strings in <" + Global.ConnectionStringFileName +
                    ">. See logfile for further details.", "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool OpenDBs()
        {
            try
            {
                Global.ModelDatabase = new ModelDatabase(modelConnectionString, Global.CurrentUser);
                Global.ModelDatabase.DeleteDB();
            }
            catch (Exception e)
            {
                this.Log<FATAL>("Error connecting to DB. Connection = " + modelConnectionString + "\n", e);
                MessageBox.Show("There was a problem connecting to the database <" + modelConnectionString +
                    ">. See logfile for further details.", "Connection error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            try
            {
                Global.UserDatabase = new UserDatabase(userConnectionString, Global.CurrentUser, true);
            }
            catch (Exception e)
            {
                this.Log<FATAL>("Error connecting to DB. Connection = " + userConnectionString + "\n", e);
                MessageBox.Show("There was a problem connecting to the database <" + userConnectionString +
                    ">. See logfile for further details.", "Connection error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private bool InitDBs()
        {
            if (!Global.ModelDatabase.Initialize())
            {
                this.Log<FATAL>("Error initializing DB. Connection = " + modelConnectionString);
                MessageBox.Show("There was a problem initializing the database <" + modelConnectionString +
                    ">. See logfile for further details.", "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!Global.UserDatabase.Initialize())
            {
                this.Log<FATAL>("Error initializing DB. Connection = " + userConnectionString);
                MessageBox.Show("There was a problem initializing the database <" + userConnectionString +
                    ">. See logfile for further details.", "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!GeoDatabase.Initialize(geoConnectionString))
            {
                this.Log<FATAL>("Unable to initialize geo database.");
                return false;
            }

            return true;
        }

        private void CloseConnections()
        {
            GeoDatabase.Close();

            try
            {
                Global.ModelDatabase.Connection.Close();
            }
            catch { /* CANNOT RECOVER */ }

            try
            {
                Global.UserDatabase.Connection.Close();
            }
            catch { /* CANNOT RECOVER */ }
        }

        #endregion
    }
}