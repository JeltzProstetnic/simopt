using System.Collections.Generic;
using System.IO;

using MatthiasToolbox.Indexer.Database;
using MatthiasToolbox.Indexer.Interfaces;

namespace MatthiasToolbox.Indexer
{
    public static class Global
    {
        #region cvar

        private static SortedDictionary<double, IDocumentResolver> injectedResolvers;

        // logging
        public static readonly long MaxLogSize = 5 * 1024 * 1024;
        public static readonly string LogFileName = "MatthiasToolbox.Indexer.log";
        public static readonly string LogFileBackupName = "MatthiasToolbox.Indexer.log.bak";

        #endregion
        #region prop

        public static string InstallationPath { get; set; }

        public static bool TestMode { get; set; }

        public static FileInfo LogFile { get; set; }

        public static IndexDatabase IndexDatabase
        {
            get
            {
                return IndexDatabase.ActiveInstance;
            }
            set
            {
                value.IsActiveInstance = true;
            }
        }

        public static IEnumerable<IDocumentResolver> InjectedResolvers
        {
            get
            {
                return injectedResolvers.Values;
            }
        }

        public static int InjectedResolverCount
        {
            get
            {
                return injectedResolvers.Count;
            }
        }

        #endregion
        #region ctor
        
        static Global()
        {
            injectedResolvers = new SortedDictionary<double, IDocumentResolver>();
        }

        #endregion
        #region impl

        public static void InjectResolver(IDocumentResolver resolver, double priority)
        {
            injectedResolvers[priority] = resolver;
        }

        #endregion
    }
}