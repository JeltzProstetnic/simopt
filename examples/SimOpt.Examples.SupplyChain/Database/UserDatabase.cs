using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.SupplyChain.Database
{
    public class UserDatabase : DataContext, ILogger
    {
        #region cvar

        // connection
        private string connectionString;
        private bool isConnected;

        // table mapping for linq to sql
        public readonly Table<Setting> SettingTable;
        public readonly Table<DatabaseLog> DatabaseLogTable;

        #endregion
        #region prop

        #region Main

        public string CurrentUser { get; private set; }
        public string ConnectionString { get { return connectionString; } }
        public bool IsConnected { get { return isConnected; } }
        public bool Initialized { get; private set; }

        #endregion
        #region Settings

        public static UserDatabase SettingsDatabase { get; set; }
        public Dictionary<string, Setting> Settings { get; private set; }
        public Dictionary<string, Type> SettingType { get; private set; }
        public Dictionary<string, string> SettingValue { get; private set; }

        #endregion
        #region Logging

        public bool LoggingEnabled { get; set; }
        public static long SessionID { get; private set; }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// Connect DataContext using given connection string and
        /// create an instance of IndexDatabase
        /// CAUTION: Further initialization required before full use. See <code>Initialize</code>.
        /// </summary>
        /// <param name="connectionString">a connection string or db filename</param>
        public UserDatabase(string connectionString, string currentUser, bool useForSettings = false)
            : base(connectionString)
        {
            Settings = new Dictionary<string, Setting>();
            SettingType = new Dictionary<string, Type>();
            SettingValue = new Dictionary<string, string>();

            CurrentUser = currentUser;
            if (useForSettings) SettingsDatabase = this;

            this.connectionString = connectionString;
            this.isConnected = true;
            this.Log<INFO>("Connected to " + base.Connection.DataSource);
        }

        #endregion
        #region init

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        /// <returns>success flag</returns>
        public bool Initialize()
        {
            if (Initialized)
            {
#if DEBUG
                this.Log<WARN>("The UserDatabase was already initialized.");
#endif
                return true;
            }

            try
            {
                // if (resetDB && DatabaseExists()) DeleteDatabase();
                if (!DatabaseExists()) MakeDB();

                if (DatabaseLogTable.Any()) SessionID = (from log in DatabaseLogTable select log.SessionID).Max() + 1;

                DatabaseLogTable.DeleteAllOnSubmit((from row in DatabaseLogTable where row.SessionID < SessionID - 2 select row));
                SubmitChanges();
            }
            catch (Exception e)
            {
                this.Log<ERROR>("Error opening DB connection or reading data: ", e);
                return false;
            }

            FillDictionaries();

            Initialized = true;
            return true;
        }

        private bool MakeDB()
        {
            try
            {
                CreateDatabase();
                SubmitChanges();
                return true;
            }
            catch (Exception ex)
            {
                this.Log<FATAL>(ex);
                return false;
            }
        }

        #endregion
        #region impl

        #region ILogger

        void ILogger.Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
        {
            if (data.ContainsKey("LogToDB") && data["LogToDB"].GetType() == typeof(bool) && (bool)data["LogToDB"] == false)
                return;

            int severity = 0;
            if (data.ContainsKey("Severity") && data["Severity"].GetType() == typeof(int))
                severity = (int)data["Severity"];

            string s = sender as string;
            if (string.IsNullOrEmpty(s))
            {
                try { s = sender.ToString(); }
                catch { s = "Unknown Sender"; }
            }

            DatabaseLog dbl = new DatabaseLog(timeStamp.Ticks, severity, messageClass.Name, message, s);

            try
            {
                DatabaseLogTable.InsertOnSubmit(dbl);
            }
            catch (Exception ex)
            {
                this.Log<ERROR>("Database logging error: ", ex, LogToDB => false);
            }
        }

        public void ShutdownLogger()
        {
            LoggingEnabled = false;
            try
            {
                SubmitChanges();
            }
            catch (Exception e)
            {
                Console.Write(e.StackTrace);
            }
        }

        #endregion

        #endregion
        #region util

        private void FillDictionaries()
        {
            if ((from row in SettingTable select row).Any())
            {
                List<Setting> settings = (from row in SettingTable where row.User == CurrentUser select row).ToList();
                foreach (Setting setting in settings)
                {
                    SettingValue[setting.Name] = setting.SettingData;
                    SettingType[setting.Name] = Type.GetType(setting.DataClass);
                    Settings[setting.Name] = setting;
                }
            }
        }

        #endregion
    }
}