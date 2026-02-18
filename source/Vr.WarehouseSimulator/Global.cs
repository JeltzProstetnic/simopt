using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vr.WarehouseSimulator.Data;

namespace Vr.WarehouseSimulator
{
    public static class Global
    {
        #region cvar

        // flags
        public static bool GodMode;

        // other
        public static readonly long MaxLogSize = 5 * 1024 * 1024;

        // filenames
        public static readonly string LogFileName = "Vr.WarehouseSimulator.log";
        public static readonly string LogFileBackupName = "Vr.WarehouseSimulator.log.bak";
        public static readonly string SimLogFileName = "Vr.WarehouseSimulator.sim.log";
        public static readonly string SimLogFileBackupName = "Vr.WarehouseSimulator.sim.log.bak";
        public static readonly string ConnectionStringFileName = "Resources\\Vr.WarehouseSimulator.con";

        // data
        private static LayoutDatabase layoutDatabase;
        private static OrderDatabase orderDatabase;
        private static ProcessDatabase processDatabase;

        #endregion
        #region prop

        /// <summary>
        /// The active Layout Database
        /// CAUTION: ONE TIME SETTER. Once set, setting again will throw an exception. Setting to null will also throw an exception.
        /// </summary>
        public static LayoutDatabase LayoutDatabase
        {
            get { return layoutDatabase; }
            internal set 
            {
                if (value == null) throw new ArgumentException("The LayoutDatabase may not be set to null!", "value");
                if (layoutDatabase != null) throw new AccessViolationException("One Time Setter: you may not set the LayoutDatabase more than once.");
                layoutDatabase = value;
            }
        }

        /// <summary>
        /// The active Order Database
        /// CAUTION: ONE TIME SETTER. Once set, setting again will throw an exception. Setting to null will also throw an exception.
        /// </summary>
        public static OrderDatabase OrderDatabase
        {
            get { return orderDatabase; }
            internal set
            {
                if (value == null) throw new ArgumentException("The OrderDatabase may not be set to null!", "value");
                if (orderDatabase != null) throw new AccessViolationException("One Time Setter: you may not set the OrderDatabase more than once.");
                orderDatabase = value;
            }
        }

        /// <summary>
        /// The active Process Database
        /// CAUTION: ONE TIME SETTER. Once set, setting again will throw an exception. Setting to null will also throw an exception.
        /// </summary>
        public static ProcessDatabase ProcessDatabase
        {
            get { return processDatabase; }
            internal set
            {
                if (value == null) throw new ArgumentException("The ProcessDatabase may not be set to null!", "value");
                if (processDatabase != null) throw new AccessViolationException("One Time Setter: you may not set the ProcessDatabase more than once.");
                processDatabase = value;
            }
        }

        #endregion
    }
}
