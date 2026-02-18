using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Logging.Loggers;
using MatthiasToolbox.Utilities;
using MatthiasToolbox.Writer.DataModel;
using MatthiasToolbox.Writer.Properties;

namespace MatthiasToolbox.Writer
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
        private string connectionString;

        // windows
        private Window window;

        #endregion
        #region main

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Non-Persistable Flags
            Global.GodMode = File.Exists("me.god");
            Global.AdminMode = File.Exists("me.admin");
            
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
                this.Log<INFO>("Logfile initialized.");
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
#if DEBUG
                Global.TestMode = true;
#else
                Global.TestMode = commandLineArgs.Contains("test");
#endif
                if (Global.TestMode) this.Log<INFO>("Running in test mode.");

                Global.ApplicationPath = SystemTools.ExecutablePath();
            }
            catch (Exception ex)
            {
                this.Log<ERROR>("There was a problem parsing the commandline arguments.", ex);
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
            bool firstStart = false;
            connectionString = Settings.Default.ConnectionString;

            try
            {
                Global.ProjectDatabase = new ProjectDatabase(connectionString, Global.CurrentUser);
                firstStart = !Global.ProjectDatabase.DatabaseExists();
                Global.ProjectDatabase.DeleteDB();
            }
            catch (Exception e)
            {
                this.Log<FATAL>("Error connecting to DB. Connection = " + connectionString + "\n", e);
                MessageBox.Show("There was a problem connecting to the database <" + connectionString +
                    ">. See logfile for further details.", "Connection error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!Global.ProjectDatabase.Initialize())
            {
                this.Log<FATAL>("Error initializing DB. Connection = " + connectionString);
                MessageBox.Show("There was a problem initializing the database <" + connectionString +
                    ">. See logfile for further details.", "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // if (firstStart) 
                CreateTemplateData();
            
            return true;
        }

        private void CloseConnections()
        {
            try
            {
                Global.ProjectDatabase.Connection.Close();
            }
            catch { /* CANNOT RECOVER */ }
        }

        private void CreateTemplateData()
        {
            Project templateProject = new Project("Default Template", SystemTools.ExecutablePath());
            Global.ProjectDatabase.ProjectTable.InsertOnSubmit(templateProject);
            Global.ProjectDatabase.SubmitChanges();

            List<Container> defaultContainers = new List<Container>() { 
                new Container(templateProject, "Time 0"), 
                new Container(templateProject, "Story 0"), 
                new Container(templateProject, "Place 0"), 
                new Container(templateProject, "Actor 0"), 
                new Container(templateProject, "Chapter 0") };
            Global.ProjectDatabase.ContainerTable.InsertAllOnSubmit(defaultContainers); // A are both codes necessary???
            Global.ProjectDatabase.SubmitChanges();

            templateProject.Containers.AddRange(defaultContainers);                     // B are both codes necessary???
        }

        #endregion
    }
}
