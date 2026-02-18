using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Player
{
    public static class Global
    {
        public static bool GodMode;

        public static long MaxLogSize = 5 * 1024 * 1024;

        public static string LogFileName = "MatthiasToolbox.Player.log";
        public static string LogFileBackupName = "MatthiasToolbox.Player.log.bak";
    }
}
