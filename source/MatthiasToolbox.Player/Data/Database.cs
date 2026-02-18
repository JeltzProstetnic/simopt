using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using MatthiasToolbox.Logging;
using System.Reflection;
using System.IO;

namespace MatthiasToolbox.Player.Data
{
    public class Database : DataContext, ILogger
    {
        #region cvar

        private static string dbFile;

        private static readonly bool RUN_COMPILED_QUERYS = true;

        #region Tables

        public readonly Table<Clip> ClipTable;
        public readonly Table<PlayList> PlayListTable;
        public readonly Table<PlayListEntry> PlayListEntryTable;

        public readonly Table<DatabaseLog> DatabaseLogTable;
        public readonly Table<Setting> SettingTable;

        #endregion

        #endregion
        #region prop

        public static Database OpenInstance { get; private set; }
        public static bool Initialized { get; private set; }
        public static long SessionID { get; private set; }

        #region ILogger

        public bool LoggingEnabled { get; set; }

        #endregion
        #region Settings

        public static Dictionary<string, Type> SettingType { get; private set; }
        public static Dictionary<string, string> SettingValue { get; private set; }
        public static Dictionary<string, Setting> Settings { get; private set; }

        #endregion

        #endregion
        #region ctor

        public Database(string connection)
            : base(connection)
        {
            OpenInstance = this;
            Settings = new Dictionary<string, Setting>();
            SettingType = new Dictionary<string, Type>();
            SettingValue = new Dictionary<string, string>();
        }

        #endregion
        #region init

        // public static void Reset() { OpenInstance.SubmitChanges(); Database db = new Database(dbFile); }

        public static bool Initialize(string databaseFile, bool resetDB = false)
        {
            if (Initialized) return true;

            String codeBase = Assembly.GetCallingAssembly().CodeBase.ToString();
            String basePath = codeBase.Substring(8, codeBase.LastIndexOf('/') - 8).Replace("/", "\\");
            dbFile = basePath + "\\" + databaseFile;

            try
            {
                Database db = new Database(dbFile);

                if (resetDB && db.DatabaseExists())
                    db.DeleteDatabase();
                if (!db.DatabaseExists()) MakeDB(); // else CreateLookups();

                if (OpenInstance.DatabaseLogTable.Count<DatabaseLog>() != 0)
                    SessionID = (from log in OpenInstance.DatabaseLogTable select log.SessionID).Max() + 1;
                else
                    SessionID = 0;

                InitializeSettings();

                OpenInstance.DatabaseLogTable.DeleteAllOnSubmit((from row in OpenInstance.DatabaseLogTable where row.SessionID < SessionID - 2 select row));
                OpenInstance.SubmitChanges();

                Initialized = true;
                Logger.Add(OpenInstance);
                OpenInstance.LoggingEnabled = true;
                return true;
            }
            catch (Exception e)
            {
                Logger.Log<ERROR>("Error opening DB connection or reading data: ", e);
                return false;
            }
        }

        private static void InitializeSettings()
        {
            if ((from row in OpenInstance.SettingTable select row).Any()) // Count() != 0
            {
                List<Setting> settings = (from row in OpenInstance.SettingTable select row).ToList();
                foreach (Setting setting in settings)
                {
                    SettingValue[setting.Name] = setting.SettingData;
                    SettingType[setting.Name] = Type.GetType(setting.DataClass);
                    Settings[setting.Name] = setting;
                }
            }
        }

        #endregion
        #region impl

        public PlayList CreatePlayList(string p)
        {
            // TODO: check existence
            PlayList pl = new PlayList(p);
            PlayListTable.InsertOnSubmit(pl);
            SubmitChanges();
            return pl;
        }

        public bool IsFileKnown(System.IO.FileInfo fi)
        {
            return RUN_COMPILED_QUERYS ? Compiled_IsFileKnownDelegate(this, fi.FullName) :
                (from row in ClipTable where row.File == fi.FullName select row).Any();
        }

        private static readonly Func<Database, string, bool> Compiled_IsFileKnownDelegate = CompiledQuery.Compile<Database, string, bool>(
            (Database db, string fullname) =>
            (from row in db.ClipTable where row.File == fullname select row).Any());

        private static void MakeDB()
        {
            Database.OpenInstance.CreateDatabase();
            Database.OpenInstance.SubmitChanges();
        }

        #region ILogger

        void ILogger.Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
        {
            if (data.ContainsKey("LogToDB") && data["LogToDB"].GetType() == typeof(bool) && (bool)data["LogToDB"] == false)
                return;

            int severity = 0;
            if (data.ContainsKey("Severity") && data["Severity"].GetType() == typeof(int))
                severity = (int)data["Severity"];

            DatabaseLog dbl = new DatabaseLog(timeStamp.Ticks, severity, messageClass.Name, message, sender.GetType().FullName);

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
                OpenInstance.SubmitChanges();
            }
            catch (Exception e)
            {
                Console.Write(e.StackTrace);
            }
        }

        #endregion

        #endregion

        public Clip GetClip(FileInfo fi)
        {
            return RUN_COMPILED_QUERYS ? Compiled_GetClip(this, fi.FullName) :
            (from row in ClipTable where row.File == fi.FullName select row).FirstOrDefault();
        }

        private static readonly Func<Database, string, Clip> Compiled_GetClip = CompiledQuery.Compile<Database, string, Clip>(
            (Database db, string fullname) =>
            (from row in db.ClipTable where row.File == fullname select row).FirstOrDefault());

        public Clip GetClip(int id)
        {
            return RUN_COMPILED_QUERYS ? Compiled_GetClipById(this, id) :
            (from row in ClipTable where row.ID == id select row).First();
        }

        private static readonly Func<Database, int, Clip> Compiled_GetClipById = CompiledQuery.Compile<Database, int, Clip>(
            (Database db, int id) =>
            (from row in db.ClipTable where row.ID == id select row).FirstOrDefault());

        /// <summary>
        /// Gets the clips of the playlist.
        /// </summary>
        /// <param name="playlistID">The playlist ID.</param>
        /// <returns></returns>
        public IEnumerable<Clip> GetClips(int playlistID)
        {
            return RUN_COMPILED_QUERYS ? Compiled_GetClipsByPlaylist(this, playlistID) :
            from o in PlayListEntryTable where o.PlayListID == playlistID join c in ClipTable on o.ClipID equals c.ID select c;
        }

        private static readonly Func<Database, int, IQueryable<Clip>> Compiled_GetClipsByPlaylist = CompiledQuery.Compile<Database, int, IQueryable<Clip>>(
            (Database db, int id) =>
            from o in db.PlayListEntryTable
            where o.PlayListID == id
            join c in db.ClipTable on o.ClipID equals c.ID
            select c
                   );

        public static readonly Func<Database, IQueryable<Clip>> GetAllClips = CompiledQuery.Compile<Database, IQueryable<Clip>>((Database db) =>
            from c in db.ClipTable select c
            );

        internal bool PlayListExists(string p)
        {
            return (from row in PlayListTable where row.Name == p select row).Any();
        }

        internal PlayList GetPlayList(string p)
        {
            return (from row in PlayListTable where row.Name == p select row).First();
        }
    }
}
