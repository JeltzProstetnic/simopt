using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Writer.DataModel
{
    public class ProjectDatabase : DataContext
    {
        /*
         static class GlobalNotifications
    {
        public static event OnChangeEventHandler OnChange;
 
        public static void InitializeNotifications(string connectString)
        {
            // Initialize notifications
            SqlDependency.Start(connectString);
            // Create and register a new dependency
            SqlDependency dependency = new SqlDependency();
            dependency.OnChange += new OnChangeEventHandler(NotificationCallback);
            System.Runtime.Remoting.Messaging.CallContext.SetData("MS.SqlDependencyCookie", dependency.Id);
        }
 
        internal static void NotificationCallback(object o, SqlNotificationEventArgs args)
        {
            OnChange.Invoke(o, args);
        }
    }
         */
        #region cvar

        #region connection

        private string userName;
        private string connectionString;
        private bool isConnected;

        #endregion
        #region tables

        public readonly Table<Project> ProjectTable;
        public readonly Table<Document> DocumentTable;
        public readonly Table<Container> ContainerTable;
        public readonly Table<Fragment> FragmentTable;
        // public readonly Table<TextBlock> TextBlockTable;
        // public readonly Table<Image> ImageTable;
        // public readonly Table<Table> TableTable;
        public readonly Table<TextFormat> TextFormatTable;
        public readonly Table<ContainerFragmentLink> ContainerFragmentLinkTable;

        #endregion

        #endregion
        #region prop

        public string CurrentUser { get { return userName; } }
        public string ConnectionString { get { return connectionString; } }
        public bool IsConnected { get { return isConnected; } }
        public bool Initialized { get; private set; }

        #endregion
        #region ctor

        /// <summary>
        /// Connect DataContext using given connection string and
        /// create an instance of the database
        /// CAUTION: Further initialization required before full use. See <code>Initialize</code>.
        /// </summary>
        /// <param name="connectionString">a connection string or db filename</param>
        public ProjectDatabase(string connectionString, string userName) : base(connectionString)
        {
            this.connectionString = connectionString;
            this.userName = userName;
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
                this.Log<WARN>("The database was already initialized.");
                return true;
            }

            if (!DatabaseExists())
            {
                try
                {

                    MakeDB();
                    SubmitChanges();
                    this.Log<INFO>("New database created.");
                }
                catch (Exception e)
                {
                    this.Log<ERROR>("Error opening DB connection or reading data: ", e);
                    return false;
                }
            }

            LoadMemoryData();

            Initialized = true;
            this.Log<INFO>("The database was successfully initialized.");
            return true;
        }

        private bool MakeDB()
        {
            try
            {
                CreateDatabase();
                CreateDefaultData();
                SubmitChanges();
                return true;
            }
            catch (Exception ex)
            {
                this.Log<FATAL>("Error creating database.", ex);
                return false;
            }
        }

        #endregion
        #region impl

        private void CreateDefaultData()
        {
        }

        private void LoadMemoryData()
        {
        }

        #endregion
        #region util

        /// <summary>
        /// deletes the db
        /// </summary>
        public void DeleteDB()
        {
            if (DatabaseExists()) DeleteDatabase();
        }

        #endregion
    }
}