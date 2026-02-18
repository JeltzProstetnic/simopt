using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Indexer.Interfaces;
using MatthiasToolbox.Utilities.Shell.ProgressDialog;
using MatthiasToolbox.Indexer.Database;
using System.IO;

namespace MatthiasToolbox.Indexer.Service
{
    internal class UpdateWorkerState
    {
        internal string ThreadName;

        internal bool useProgressDialog;
        internal bool useLogging = false;
        internal bool useServiceCallback = false;
        internal IUpdateCallback callback = null;
        internal OperationsProgressDialog OperationsDialog;
        internal ProgressDialog Dialog;
        internal ulong currentItemSize;

        internal IndexDatabase indexDatabase;
        internal Queue<FileInfo> workLoad;
        internal ulong workLoadBytes;
    }
}
