using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Writer.DataModel;

namespace MatthiasToolbox.Writer
{
    public static class Global
    {
        #region cvar

        // flags
        public static bool GodMode;
        public static bool TestMode;
        public static bool AdminMode;

        // other
        public static readonly long MaxLogSize = 5 * 1024 * 1024;

        // filenames
        public static readonly string LogFileName = "Writer.log";
        public static readonly string LogFileBackupName = "Writer.log.bak";

        // directories
        public static string ApplicationPath;

        // data
        private static ProjectDatabase projectDatabase;

        #endregion
        #region prop

        /// <summary>
        /// the user which is currently logged onto windows
        /// </summary>
        public static string CurrentUser { get { return Environment.UserDomainName + "\\" + Environment.UserName; } }

        /// <summary>
        /// The active Model Database
        /// CAUTION: ONE TIME SETTER. Once set, setting again will throw an exception. Setting to null will also throw an exception.
        /// </summary>
        public static ProjectDatabase ProjectDatabase
        {
            get { return projectDatabase; }
            internal set
            {
                if (value == null) throw new ArgumentException("The ProjectDatabase may not be set to null!", "value");
                if (projectDatabase != null) throw new AccessViolationException("One Time Setter: you may not set the ProjectDatabase more than once.");
                projectDatabase = value;
            }
        }

        #endregion
    }
}
