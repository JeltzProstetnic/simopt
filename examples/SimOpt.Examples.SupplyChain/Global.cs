using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Geography;
using MatthiasToolbox.SupplyChain.Database;

namespace MatthiasToolbox.SupplyChain
{
    class Global
    {
        #region cvar

        // flags
        public static bool GodMode;
        public static bool TestMode;
        public static bool AdminMode;

        // other
        public static readonly long MaxLogSize = 5 * 1024 * 1024;

        // filenames
        public static readonly string LogFileName = "SupplyChainSimulator.log";
        public static readonly string LogFileBackupName = "SupplyChainSimulator.log.bak";
        public static readonly string ConnectionStringFileName = "Resources\\SupplyChainSimulator.con";

        // directories
        public static string ApplicationPath;

        // data
        private static ModelDatabase modelDatabase;
        private static UserDatabase userDatabase;
        
        // headers
        public static readonly string CustomerTabTitle = "Patients";
        public static readonly string ProductTabTitle = "Medication";
        public static readonly string SalesTabTitle = "Trial Schedule";

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
        public static ModelDatabase ModelDatabase
        {
            get { return modelDatabase; }
            internal set
            {
                if (value == null) throw new ArgumentException("The ModelDatabase may not be set to null!", "value");
                if (modelDatabase != null) throw new AccessViolationException("One Time Setter: you may not set the ModelDatabase more than once.");
                modelDatabase = value;
            }
        }

        /// <summary>
        /// The active User Database
        /// CAUTION: ONE TIME SETTER. Once set, setting again will throw an exception. Setting to null will also throw an exception.
        /// </summary>
        public static UserDatabase UserDatabase
        {
            get { return userDatabase; }
            internal set
            {
                if (value == null) throw new ArgumentException("The UserDatabase may not be set to null!", "value");
                if (userDatabase != null) throw new AccessViolationException("One Time Setter: you may not set the UserDatabase more than once.");
                userDatabase = value;
            }
        }

        #endregion
    }
}
