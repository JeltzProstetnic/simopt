using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Money
{
    public static class Global
    {
        public static bool GodMode;

        public static long MaxLogSize = 5 * 1024 * 1024;

        public static string LogFileName = "MatthiasToolbox.Money.log";
        public static string LogFileBackupName = "MatthiasToolbox.Money.log.bak";
    }
}
