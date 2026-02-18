using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceProcess;
using System.Threading;
using System.Windows;
using System.Windows.Documents;

using MatthiasToolbox.Indexer.Attributes;
using MatthiasToolbox.Indexer.Database;
using MatthiasToolbox.Indexer.Enumerations;
using MatthiasToolbox.Indexer.Interfaces;
using MatthiasToolbox.Indexer.Properties;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Logging.Loggers;
using MatthiasToolbox.Utilities;
using MatthiasToolbox.Utilities.Shell.ProgressDialog;

namespace MatthiasToolbox.Indexer.Service
{
    /// <summary>
    /// Default service name is "MatthiasToolbox.Indexer.IndexUpdateService"
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)] // Test single instead of PerSession
    public class IndexUpdateService : ServiceBase, IUpdateService
    {
        #region cvar

        // locks
        private static object lockObject = new object();
        private object workerLock = new object();

        // instances
        private int instanceNumber;
        private static int instanceCounter = 0;
        private static Dictionary<int, IndexUpdateService> instances;
        private IndexUpdateService instance;

        // async worker
        private List<BackgroundWorker> workers = new List<BackgroundWorker>();
        private bool startAnnounced;
        private bool finishAnnounced;
        private ulong finishedFiles;

        // environment
        private static OperatingSystem os = Environment.OSVersion;

        // wcf service
        private ServiceHost serviceHost = null;

        // startup
        private List<string> startupArgs;
        private bool resetDB = false;

        // timer
        private Timer serviceTimer;
        private DateTime lastUpdateDateTime;
        private DateTime lastCheckDateTime;

        #endregion
        #region prop

        /// <summary>
        /// Indicates if the server is currently busy indexing.
        /// </summary>
        public static bool CurrentlyIndexing { get; set; }

        /// <summary>
        /// Returns true if the time interval for index refreshing is dividable by 24hrs.
        /// </summary>
        public static bool IsIntervalInWholeDays
        {
            get { return (int)Settings.Default.IndexUpdatingInterval.TotalHours % 24 == 0; }
        }

        private Queue<FileInfo> FilesToDo = new Queue<FileInfo>();

        /// <summary>
        /// Temporary value, will be updated in StartIndexing.
        /// </summary>
        private ulong TotalSize { get; set; }

        /// <summary>
        /// Temporary value, will be updated in StartIndexing.
        /// </summary>
        private ulong NumberOfFilesToProcess { get; set; }

        #endregion
        #region ctor

        static IndexUpdateService() { instances = new Dictionary<int, IndexUpdateService>(); }

        /// <summary>
        /// Default service name is "MatthiasToolbox.Indexer.IndexUpdateService"
        /// </summary>
        public IndexUpdateService()
        {
            this.instanceNumber = instanceCounter++;
            instances[this.instanceNumber] = this;
            instance = this;

            Global.InstallationPath = SystemTools.ExecutablePath();

            this.ServiceName = Settings.Default.ServiceName;
            this.EventLog.Log = Settings.Default.EventLogName;

            this.CanHandlePowerEvent = true;
            this.CanHandleSessionChangeEvent = true;
            this.CanPauseAndContinue = true;
            this.CanShutdown = true;
            this.CanStop = true;

            SetupLogging();
        }

        #endregion
        #region dtor

        /// <summary>
        /// Removes the instance from the static dictionary.
        /// </summary>
        ~IndexUpdateService()
        {
            instances.Remove(instanceNumber);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">true: release also managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #endregion
        #region main

        /// <summary>
        /// Main Service Thread: This is where the service is run.
        /// </summary>
        static void Main()
        {
            ServiceBase.Run(new IndexUpdateService());
        }

        #endregion
        #region impl

        #region startup

        /// <summary>
        /// Initialize the WCF service host and endpoints.
        /// </summary>
        private void InitializeWCF()
        {
            // Initialize Service Host
            if (serviceHost != null) serviceHost.Close();

            // Create a ServiceHost for the CalculatorService type and provide the base address.
            //serviceHost = new ServiceHost(typeof(IndexUpdateService));

            // base address and endpoints are already defined in config file
            Uri baseAddress = new Uri("http://localhost:8000/Indexer/Service");
            serviceHost = new ServiceHost(typeof(IndexUpdateService), baseAddress);
            try
            {
                serviceHost.AddServiceEndpoint(typeof(IUpdateService), new WSDualHttpBinding(), baseAddress);
                ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                smb.HttpGetEnabled = true;
                serviceHost.Description.Behaviors.Add(smb);
            }
            catch (CommunicationException ce)
            {
                Console.WriteLine("An exception occurred: {0}", ce.Message);
                serviceHost.Abort();
            }
            
            // Open the ServiceHostBase to create listeners and start listening for messages.
            serviceHost.Open();
        }

        /// <summary>
        /// initialize logger with a PlainTextFile logger
        /// and a ConsoleLogger (only in DEBUG mode)
        /// </summary>
        /// <returns>success flag</returns>
        private bool SetupLogging()
        {
            try
            {
                // create file infos
                Global.LogFile = new FileInfo(Global.InstallationPath + "\\" + Global.LogFileName);

                // backup and delete old logfiles
                if (Global.LogFile.Exists && Global.LogFile.Length > Global.MaxLogSize)
                    File.Copy(Global.LogFile.FullName, Global.InstallationPath + "\\" + Global.LogFileBackupName, true);

                // create and start application log file
                Logger.Add(new PlainTextFileLogger(Global.LogFile));
#if DEBUG
                Logger.Add(new ConsoleLogger());
#endif
                this.Log<INFO>("Logging successfully initialized. Main logging target is <" + Global.LogFile.FullName + ">.");
            }
            catch (IOException ex) 
            {
                this.Log<INFO>("Unable to access logfile. Usually this means that an instance is already running.");
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create IndexDatabase instance and set it to active.
        /// </summary>
        /// <returns></returns>
        private bool InitializeDatabase()
        {
            try
            {
                string dbPath = Settings.Default.IndexConnectionString;
                if (!dbPath.Contains("\\")) dbPath = Global.InstallationPath + "\\" + dbPath;
                Global.IndexDatabase = new IndexDatabase(dbPath);
                Global.IndexDatabase.Initialize(resetDB);
                return true;
            }
            catch (Exception e)
            {
                this.Log<ERROR>("Unable to initialize index database.", e);
                return false;
            }
        }

        /// <summary>
        /// parse and interpret startup args
        /// </summary>
        /// <param name="e">the arguments provided by Start</param>
        /// <returns>success flag</returns>
        private bool ParseStartupArguments(string[] args)
        {
            try
            {
                List<string> commandLineArgsOriginal = args.ToList<string>();
                startupArgs = new List<string>();
                foreach (string arg in commandLineArgsOriginal)
                    startupArgs.Add(arg.ToLower().Replace("-", "").Replace("/", "").Trim());
                Global.TestMode = startupArgs.Contains("test");
#if DEBUG
                Global.TestMode = true;
#endif
                if (Global.TestMode) this.Log<INFO>("Indexing Server running in test mode.");
                resetDB = File.Exists(Global.InstallationPath + "\\reset.me");
                return true;
            }
            catch (Exception ex)
            {
                this.Log<ERROR>("A problem occurred while parsing the startup arguments.", ex);
                return false;
            }
        }

        /// <summary>
        /// Look for external custom document resolvers.
        /// </summary>
        private void LoadExternalResolvers()
        {
            if (File.Exists(Global.InstallationPath + "\\" + Settings.Default.DocumentResolverPluginName))
            {
                // get file info
                FileInfo fi = new FileInfo(Global.InstallationPath + "\\" + Settings.Default.DocumentResolverPluginName);
                this.Log<INFO>("Found a document resolver plugin library at <" + fi.FullName + ">.");

                // load assembly
                Assembly assembly = Assembly.LoadFrom(fi.FullName);
                this.Log<INFO>("Assembly loaded succesfully.");

                // find document resolver attributes
                var q = (from entry in assembly.GetTypes()
                         where entry.GetCustomAttributes(typeof(DocumentResolverAttribute), false).Any()
                         select entry);

                // create instances
                foreach(Type t in q)
                {
                    this.Log<INFO>("Document resolver attribute found at " + t.FullName + ".");
                    
                    ConstructorInfo ci = t.GetConstructor(new Type[] { });

                    if (ci == null)
                    {
                        this.Log<ERROR>("The marked class cannot be used as document resolver because it does not provide a parameterless constructor.");
                    }
                    else
                    {
                        IDocumentResolver customResolver = (IDocumentResolver)ci.Invoke(null);
                        // IDocumentResolver customResolver = (IDocumentResolver)Activator.CreateInstance(t, new Type[] { });
                        Global.InjectResolver(customResolver, customResolver.Priority);
                    }
                }

                this.Log<INFO>(Global.InjectedResolverCount.ToString() + " custom resolver initialized.");
            }
        }

        /// <summary>
        /// Configure and start the timer.
        /// </summary>
        private void StartThreadingTimer()
        {
            int interval;
            lastUpdateDateTime = Settings.Default.LastUpdate;
            lastCheckDateTime = DateTime.Now;
            if (IsIntervalInWholeDays) interval = 900000; // check every 15 minutes
            else interval = (int)Settings.Default.IndexUpdatingInterval.TotalMilliseconds;
            serviceTimer = new Timer(ServiceTimerCallback, null, 30000, interval); // wait 30 seconds befor launching the timer
        }

        /// <summary>
        /// Start a gui application on the current desktop.
        /// </summary>
        private void StartDesktopApplication()
        {
            // TODO: this doesn't work so easy. It has to be started as user. ?WindowsIdentity.Impersonate? ?ProcessStartInfo?

            //if (!String.IsNullOrEmpty(Settings.Default.ControllerApplication)) 
            //    Process.Start(Global.InstallationPath + "\\" + Settings.Default.ControllerApplication);

            #region API code
            /*
             * enum WTS_CONNECTSTATE_CLASS
{
WTSActive,
WTSConnected,
WTSConnectQuery, // This was misspelled.
WTSShadow,
WTSDisconnected,
WTSIdle,
WTSListen,
WTSReset,
WTSDown,
WTSInit
};

[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
struct WTS_SESSION_INFO
{
public int SessionId;
public string pWinStationName;
public WTS_CONNECTSTATE_CLASS State;
}

You need to define the function differently as well as make some
corrections.

[DllImport("wtsapi32.dll", CharSet=CharSet.Auto)]
private static extern bool WTSEnumerateSessions(
// Always use IntPtr for handles, and then something derived from
SafeHandle in .NET 2.0
IntPtr hServer,
// Not required, but a good practice.
[MarshalAs(UnmanagedType.U4)]
int Reserved,
[MarshalAs(UnmanagedType.U4)]
int Version,
// You are going to create the memory block yourself.
ref IntPtr ppSessionInfo,
[MarshalAs(UnmanagedType.U4)]
ref int pCount);

You also need the following definition:

[DllImport("wtsapi32.dll")]
private static extern void WTSFreeMemory(IntPtr pMemory);

Now, to make the call, you do this:

// Create the pointer that will get the buffer.
IntPtr buffer = IntPtr.Zero;

// The count.
int count = 0;

// Make the call.
if (WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref buffer, ref count))
{
// Marshal to a structure array here. Create the array first.
WTS_SESSION_INFO[] sessionInfo = new WTS_SESSION_INFO[count];

// Cycle through and copy the array over.
for (int index = 0; index < count; index++)
// Marshal the value over.
sessionInfo[index] = Marshal.PtrToStructure(buffer +
(sizeof(WTS_SESSION_INFO) * index), typeof(WTS_SESSION_INFO));

// Work with the array here.
}

// Close the buffer.
WTSFreeMemory(buffer);
             */
            #endregion
        }

        #endregion
        #region commands

        private void UpdateIndex()
        {
            lock (lockObject) StartIndexing(false, true, true);
        }

        private void CleanupIndex()
        {
            lock (lockObject)
            {
                if (CurrentlyIndexing) return;

                if (Settings.Default.CurrentDocuments == null)
                {
                    Settings.Default.CurrentDocuments = new System.Collections.Specialized.StringCollection();
                    Settings.Default.Save();
                }

                if (Settings.Default.CurrentDocuments.Count != 0)
                {
                    foreach (string path in Settings.Default.CurrentDocuments)
                    {
                        if (Global.IndexDatabase.DocumentExists(path))
                            Global.IndexDatabase.GetDocument(path).Delete();
                    }
                    Settings.Default.CurrentDocuments.Clear();
                    Settings.Default.Save();
                }
            }
        }

        private void CancelWorker()
        {
            lock (lockObject)
            {
                if (CurrentlyIndexing)
                {
                    foreach (IndexUpdateService s in instances.Values)
                    {
                        foreach(BackgroundWorker w in s.workers) w.CancelAsync();
                    }
                }
            }
        }

        #endregion
        #region system events

        /// <summary>
        /// System shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            base.OnShutdown();
            CancelWorker();
            Global.IndexDatabase.Close();
        }

        /// <summary>
        /// OnPowerEvent(): Useful for detecting power status changes,
        ///   such as going into Suspend mode or Low Battery for laptops.
        /// </summary>
        /// <param name="powerStatus">The Power Broadcast Status
        /// (BatteryLow, Suspend, etc.)</param>
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            if (powerStatus == PowerBroadcastStatus.QuerySuspend)
            {
                CancelWorker();
                Global.IndexDatabase.Close();
            }
            return base.OnPowerEvent(powerStatus);
        }

        /// <summary>
        /// OnSessionChange(): To handle a change event
        ///   from a Terminal Server session.
        ///   Useful if you need to determine
        ///   when a user logs in remotely or logs off,
        ///   or when someone logs into the console.
        /// </summary>
        /// <param name="changeDescription">The Session Change
        /// Event that occured.</param>
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
        }

        #endregion
        #region service events

        /// <summary>
        /// Startup code.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

#if DEBUG
            if (File.Exists(Global.InstallationPath + "\\debug.me")) Debugger.Launch();
#endif
            // Initialize the WCF service host and endpoints.
            InitializeWCF();

            // Import Commandline Arguments
            if (!ParseStartupArguments(args)) throw new ApplicationException("Error parsing the startup arguments.");

            // Initialize Database
            if (!InitializeDatabase()) throw new ApplicationException("Error initializing the index database.");

            // check for dependency injection
            LoadExternalResolvers();

            // create timer
            StartThreadingTimer();

            // start GUI
            StartDesktopApplication();
        }
        
        /// <summary>
        /// Receive a command
        /// 
        /// A custom command can be sent to a service by using this method:
        /// int command = 128; //Some Arbitrary number between 128 & 256
        /// ServiceController sc = new ServiceController("NameOfService");
        /// sc.ExecuteCommand(command);
        /// </summary>
        /// <param name="command">Arbitrary Integer between 128 & 256</param>
        protected override void OnCustomCommand(int command)
        {
            if (command > 128 && command < 133)
            {
                ServiceCommand cmd = (ServiceCommand)command;
                switch (cmd)
                {
                    case ServiceCommand.Test:
                        this.Log<INFO>(Settings.Default.ServiceDisplayName + " received test command.");
                        break;
                    case ServiceCommand.Update:
                        UpdateIndex();
                        return;
                    case ServiceCommand.Cleanup:
                        CleanupIndex();
                        return;
                    case ServiceCommand.Shutdown:
                        ServiceController sc = new ServiceController(ServiceName);
                        sc.Stop(); // CanStop is true for this service.
                        return;
                    case ServiceCommand.Cancel:
                        CancelWorker();
                        return;
                    default:
                        this.Log<WARN>("An unknown custom command was received.");
                        return;
                }
            }
            base.OnCustomCommand(command);
        }

        /// <summary>
        /// Stopping code.
        /// </summary>
        protected override void OnStop()
        {
            base.OnStop();

            CancelWorker();

            if (serviceHost != null)
            {
                serviceHost.Close();
                serviceHost = null;
            }

            Global.IndexDatabase.Close();
        }

        /// <summary>
        /// Handle suspension.
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();
        }

        /// <summary>
        /// Resume.
        /// </summary>
        protected override void OnContinue()
        {
            base.OnContinue();
        }

        #endregion
        #region IUpdateService

        public bool IsUpdateRunning()
        {
            lock (lockObject) return CurrentlyIndexing;
        }

        public bool StartUpdate()
        {
            lock (lockObject)
            {
                CleanupIndex();
                if (Environment.ProcessorCount > 1 && Settings.Default.MultiThreadingEnabled) StartMultiCoreIndexing(false, true, true);
                else StartIndexing(false, true, true);
            }

            return true;
        }

        public void CancelUpdate()
        {
            CancelWorker();
        }

        public void Cleanup()
        {
            CleanupIndex();
        }

        #endregion
        #region indexing

        #region ProcessFile utils

        private bool GetCRC(FileInfo file, out int crc)
        {
            crc = 0;

            try
            {
                crc = file.GetCRC32();
            }
            catch (Exception ex)
            {
                this.Log<ERROR>("Unable to check CRC for <" + file.FullName + ">.", ex);
                this.Log<ERROR>(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                    this.Log<ERROR>(ex.InnerException.StackTrace);
                }
                return false;
            }

            return true;
        }

        private Document GetOrCreateDocument(bool changedDocument, int crc, string path, IndexDatabase db)
        {
            Document doc = null;

            if (changedDocument)
            {
                try
                {
                    doc = Global.IndexDatabase.GetDocument(path);
                    doc.Checksum = crc;
                }
                catch (Exception ex) 
                {
                    this.Log<ERROR>("Unable to retrieve document from DB: ", ex);
                    this.Log<ERROR>(ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                        this.Log<ERROR>(ex.InnerException.StackTrace);
                    }
                }
            }
            else doc = new Document(crc, path);

            return doc;
        }

        private bool ResolveDocument(Document doc, ref IEnumerable<IVariableContainer<string, string>> metaData)
        {
            bool resolved = false;
            foreach (IDocumentResolver resolver in Global.InjectedResolvers)
            {
                if (resolver.CanResolve(doc))
                {
                    doc.Data = resolver.Resolve(doc);
                    metaData = resolver.CurrentMetaData;
                    doc.HasData = true;
                    resolved = true;
                    break;
                }
            }
            return resolved;
        }

        private bool AddOrUpdate(IndexDatabase db, Document doc, bool changedDocument, FileInfo file)
        {
            try
            {
                if (changedDocument) db.UpdateDocument(doc, true); 
                else db.AddDocument(doc, true);
            }
            catch (Exception ex)
            {
                string s = "add";
                if (changedDocument) s = "update";
                this.Log<ERROR>("Unable to " + s + " document <" + file.FullName + "> - ", ex);
                this.Log<ERROR>(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                    this.Log<ERROR>(ex.InnerException.StackTrace);
                }
                return false;
            }
            return true;
        }

        private void SortFilesSingleCore()
        {
            // freshest first
            List<FileInfo> seq = FilesToDo.OrderByDescending(f => f.LastWriteTime.Year).ToList();
            FilesToDo.Clear();
            foreach (FileInfo f in seq) FilesToDo.Enqueue(f);
        }

        private void SortFilesMultiCore()
        {
            Random rnd = new Random();
            // random order, prefer fresh docs
            List<FileInfo> seq = FilesToDo.OrderBy(f => (int)(rnd.NextDouble() * 10)).ThenByDescending(f => f.LastWriteTime.Year).ToList();
            FilesToDo.Clear();
            foreach (FileInfo f in seq) FilesToDo.Enqueue(f);
        }

        private bool SubmitMetaData(IndexDatabase db, FileInfo file, Document doc, ref IEnumerable<IVariableContainer<string, string>> metaData)
        {
            try
            {
                foreach (IVariableContainer<string, string> m in metaData) doc.AddMetaData(m, false, db);
                db.SubmitChanges();
            }
            catch (Exception ex)
            {
                this.Log<WARN>("Error processing metadata in <" + file.FullName + ">. " + ex.Message);
                this.Log<ERROR>(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                    this.Log<ERROR>(ex.InnerException.StackTrace);
                }
                return false;
            }

            return true;
        }

        #endregion
        #region single core

        /// <summary>
        /// Process the given file (add to index or update data).
        /// </summary>
        /// <param name="file"></param>
        private bool ProcessFile(FileInfo file, bool log = false)
        {
            IEnumerable<IVariableContainer<string, string>> metaData = null;

            this.Log<STATUS>("Processing file <" + file.FullName + ">.");

            int crc;
            bool changedDocument;
            if(!GetCRC(file, out crc)) return false;
            string path = file.FullName;

            // doc already exists -> no changes, return. otherwise check if changes are present.
            try
            {
                if (Global.IndexDatabase.DocumentExists(crc, path)) return true;
                changedDocument = Global.IndexDatabase.DocumentExists(path);
            }
            catch (Exception ex)
            {
                this.Log<ERROR>("Unable to check document status for <" + path + "> - ", ex);
                this.Log<ERROR>(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                    this.Log<ERROR>(ex.InnerException.StackTrace);
                }
                return false;
            }

            if (Global.InjectedResolverCount > 0) // use special resolvers
            {
                Document doc = GetOrCreateDocument(changedDocument, crc, path, Global.IndexDatabase);
                if (doc == null) return false;

                bool resolved = ResolveDocument(doc, ref metaData);

                if (!resolved && !Settings.Default.UseDefaultResolversAsFallback) // do not use default resolvers
                {
                    if (log) this.Log<WARN>("<" + file.FullName + "> remains unresolved.");
                    return true;
                }

                Settings.Default.CurrentDocuments.Add(path);
                Settings.Default.Save();

                if (!AddOrUpdate(Global.IndexDatabase, doc, changedDocument, file)) return false;
                if (!SubmitMetaData(Global.IndexDatabase, file, doc, ref metaData)) return false;

                Settings.Default.CurrentDocuments.Remove(path);
                Settings.Default.Save();
            }
            else // use default resolvers
            {
                ResolveDefault(path, changedDocument, crc, file);
            }

            if (log)
            {
                if (changedDocument)
                    this.Log<INFO>("Updated file <" + file.FullName + ">.");
                else
                    this.Log<INFO>("Indexed file <" + file.FullName + ">.");
            }

            return true;
        }

        private bool ResolveDefault(string path, bool changedDocument, int crc, FileInfo file)
        {
            Settings.Default.CurrentDocuments.Add(path);
            Settings.Default.Save();

            if (changedDocument)
            {
                Document doc = null;

                try
                {
                    doc = Global.IndexDatabase.GetDocument(path);
                    doc.Checksum = crc;
                }
                catch (Exception ex)
                {
                    this.Log<WARN>("<" + file.FullName + "> not retrieved. " + ex.Message);
                    this.Log<ERROR>(ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                        this.Log<ERROR>(ex.InnerException.StackTrace);
                    }
                    return false;
                }

                try
                {
                    Global.IndexDatabase.UpdateDocument(doc);
                }
                catch (Exception ex)
                {
                    this.Log<WARN>("<" + file.FullName + "> not updated. " + ex.Message);
                    this.Log<ERROR>(ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                        this.Log<ERROR>(ex.InnerException.StackTrace);
                    }
                    return false;
                }
            }
            else
            {
                try
                {
                    Global.IndexDatabase.AddDocument(crc, path, -1, null, true);
                }
                catch (Exception ex)
                {
                    this.Log<WARN>("<" + file.FullName + "> not added. " + ex.Message);
                    this.Log<ERROR>(ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                        this.Log<ERROR>(ex.InnerException.StackTrace);
                    }
                    return false;
                }
            }

            Settings.Default.CurrentDocuments.Remove(path);
            Settings.Default.Save();

            return true;
        }

        /// <summary>
        /// Prepare and start the index updating thread.
        /// </summary>
        /// <param name="useProgress"></param>
        private void StartIndexing(bool useProgress, bool useCallback, bool useLogging)
        {
            if (CurrentlyIndexing) return;
            CurrentlyIndexing = true;

            workers.Clear();

            UpdateWorkerState state = new UpdateWorkerState();
            state.useServiceCallback = useCallback;
            state.useProgressDialog = useProgress;
            state.useLogging = useLogging;

            if (useCallback) state.callback = OperationContext.Current.GetCallbackChannel<IUpdateCallback>();

            TotalSize = 0;
            NumberOfFilesToProcess = 0;

            RunUpdater(state);
        }

        /// <summary>
        /// Launch the index updater thread.
        /// </summary>
        /// <param name="userState"></param>
        private void RunUpdater(UpdateWorkerState userState)
        {
            if (userState.useProgressDialog)
            {
                if (os.IsAtLeastWindowsVista()) userState.OperationsDialog.ShowDialog();
                else userState.Dialog.ShowDialog();
            }

            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = userState.useProgressDialog;

            worker.RunWorkerCompleted += UpdateThread_RunWorkerCompleted;
            worker.DoWork += UpdateThread_DoWork;

            if (userState.useProgressDialog || userState.useServiceCallback)
                worker.ProgressChanged += UpdateThread_ProgressChanged;

            workers.Add(worker);
            worker.RunWorkerAsync(userState);
        }

        private void UpdateThread_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            UpdateWorkerState workerState = (UpdateWorkerState)e.Argument;

            if (workerState.useServiceCallback)
                workerState.callback.NotifyStart(NumberOfFilesToProcess, TotalSize);

            #region select files

            foreach (string s in Settings.Default.DataPaths)
            {
                DirectoryInfo dir = new DirectoryInfo(s);
                foreach (FileInfo fi in dir.AllFiles())
                {
                    if (Global.InjectedResolverCount != 0)
                    {
                        foreach (IDocumentResolver resolver in Global.InjectedResolvers)
                        {
                            if (resolver.CanResolve(new Document(fi)))
                            {
                                FilesToDo.Enqueue(fi);
                                break;
                            }
                        }
                    }
                    else FilesToDo.Enqueue(fi);
                }
            }

            #endregion
            #region count files

            foreach (FileInfo file in FilesToDo)
            {
                NumberOfFilesToProcess++;
                TotalSize += (ulong)file.Length;
            }

            SortFilesSingleCore();

            #endregion
            #region setup worker dialog

            if (workerState.useProgressDialog)
            {

                if (os.IsAtLeastWindowsVista())
                {
                    #region create dialog

                    OperationsProgressDialog operationsProgressDialog = new OperationsProgressDialog();

                    operationsProgressDialog.CancelMessage = "Cancelling operation...";
                    // operationsProgressDialog.Action = OperationAction.Copying;
                    operationsProgressDialog.TotalCount = NumberOfFilesToProcess;
                    operationsProgressDialog.Message = "Updating the index.";
                    operationsProgressDialog.TargetPath = "Index Database";
                    operationsProgressDialog.SourcePath = "Index Source";
                    operationsProgressDialog.Caption = "Updating Index";
                    operationsProgressDialog.Title = "Updating Index";
                    operationsProgressDialog.DisplaysSourcePath = true;
                    operationsProgressDialog.DisplaysTargetPath = true;
                    operationsProgressDialog.ShowsTimeRemaining = true;
                    operationsProgressDialog.HasMinimizeButton = false;
                    operationsProgressDialog.HasCancelButton = true;
                    operationsProgressDialog.HasPauseButton = false;
                    operationsProgressDialog.HasProgressBar = true;
                    operationsProgressDialog.CompactsPaths = true;
                    operationsProgressDialog.AllowsUndo = false;
                    operationsProgressDialog.IsModal = true;
                    operationsProgressDialog.TotalSize = TotalSize;
                    operationsProgressDialog.TotalValue = 100;
                    operationsProgressDialog.Count = 0; //
                    operationsProgressDialog.Value = 0;
                    operationsProgressDialog.Size = 0; //

                    workerState.OperationsDialog = operationsProgressDialog;

                    #endregion
                }
                else
                {
                    #region create dialog

                    ProgressDialog progressDialog = new ProgressDialog();

                    progressDialog.ShowsTimeRemaining = true;
                    progressDialog.HasMinimizeButton = false;
                    progressDialog.HasCancelButton = true;
                    progressDialog.HasProgressBar = true;
                    progressDialog.CompactsPaths = true;
                    progressDialog.TotalValue = 100;
                    progressDialog.IsModal = true;
                    progressDialog.Value = 0;

                    workerState.Dialog = progressDialog;

                    #endregion
                }
            }

            #endregion

            ulong i = 0;
            while (FilesToDo.Count > 0)
            {
                FileInfo file = FilesToDo.Dequeue();

                #region progress update

                try
                {
                    if (worker.CancellationPending) break;

                    workerState.currentItemSize = (ulong)file.Length;
                    int percentage = (int)((i * 100ul) / NumberOfFilesToProcess);

                    if (workerState.useProgressDialog)
                        worker.ReportProgress(percentage, e.Argument);

                    if (workerState.useServiceCallback)
                        workerState.callback.ReportProgress(percentage, workerState.currentItemSize);
                }
                catch(Exception ex) 
                {
                    this.Log<WARN>("Threading error: " + ex.Message);
                    this.Log<ERROR>(ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                        this.Log<ERROR>(ex.InnerException.StackTrace);
                    }
                }

                #endregion

                i++;

                if (!ProcessFile(file, workerState.useLogging))
                {
                    #region cleanup

                    bool check = true;

                    try 
                    { 
                        if (Global.IndexDatabase.DocumentExists(file.FullName))
                            Global.IndexDatabase.GetDocument(file.FullName).Delete();
                    }
                    catch(Exception ex) 
                    {
                        check = false;

                        this.Log<ERROR>("Unable to delete partially processed document: ", ex);
                        this.Log<ERROR>(ex.StackTrace);
                        if (ex.InnerException != null)
                        {
                            this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                            this.Log<ERROR>(ex.InnerException.StackTrace);
                        }
                    }

                    try
                    {
                        Settings.Default.CurrentDocuments.Remove(file.FullName);
                        Settings.Default.Save();
                    }
                    catch(Exception ex) 
                    {
                        check = false;

                        this.Log<ERROR>("Cleanup error: ", ex);
                        this.Log<ERROR>(ex.StackTrace);
                        if (ex.InnerException != null)
                        {
                            this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                            this.Log<ERROR>(ex.InnerException.StackTrace);
                        }
                    }

                    if (check) FilesToDo.Enqueue(file);

                    #endregion
                }
            }

            if (workerState.useLogging) this.Log<INFO>("Finished.");

            try
            {
                if (workerState.useProgressDialog) worker.ReportProgress(100, e.Argument);
            }
            catch { }

            e.Result = e.Argument;
        }

        private void UpdateThread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                BackgroundWorker worker = (BackgroundWorker)sender;
                UpdateWorkerState workerState = (UpdateWorkerState)e.UserState;

                // Update progress bar
                if (os.IsAtLeastWindowsVista())
                {
                    workerState.OperationsDialog.Value = (ulong)e.ProgressPercentage;
                    workerState.OperationsDialog.Size += workerState.currentItemSize;
                    workerState.OperationsDialog.Count += 1;
                    // Cancel the operation if Cancel button has been pressed
                    if (workerState.OperationsDialog.IsCancelled) worker.CancelAsync();
                }
                else
                {
                    workerState.Dialog.Value = (ulong)e.ProgressPercentage;
                    // Cancel the operation if Cancel button has been pressed
                    if (workerState.Dialog.IsCancelled) worker.CancelAsync();
                }
            }
            catch (Exception ex) 
            {
                this.Log<ERROR>("Unable to update the progress dialog: ", ex);
                this.Log<ERROR>(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                    this.Log<ERROR>(ex.InnerException.StackTrace);
                }
            }
        }

        private void UpdateThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker worker = null;
            UpdateWorkerState state = null;

            try
            {
                worker = (BackgroundWorker)sender; // not needed
                state = (UpdateWorkerState)e.Result;

                if (e.Error != null)
                {
                    this.Log<ERROR>("Worker thread error: ", e.Error);
                    this.Log<ERROR>(e.Error.StackTrace);
                    if (e.Error.InnerException != null) instance.Log<ERROR>("Inner Exception: ", e.Error.InnerException);
                    this.Log<ERROR>(e.Error.InnerException.StackTrace);
                }

                if (state.useProgressDialog)
                {
                    if (os.IsAtLeastWindowsVista()) state.OperationsDialog.CloseDialog();
                    else state.Dialog.CloseDialog();
                    if (e.Error != null) MessageBox.Show(e.Error.Message);
                }
            }
            catch (Exception ex)
            {
                this.Log<ERROR>("Error in update thread: ", ex);
                this.Log<ERROR>(ex.StackTrace);
                if (ex.InnerException != null) this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                this.Log<ERROR>(ex.InnerException.StackTrace);
            }
            finally
            {
                lock (lockObject)
                {
                    worker = null;
                    CurrentlyIndexing = false;
                    if (state != null && state.useServiceCallback)
                        state.callback.NotifyFinish(e.Cancelled);
                }
            }
        }

        #endregion
        #region multi core

        /// <summary>
        /// Process the given file (add to index or update data).
        /// For multithreading
        /// </summary>
        /// <param name="file"></param>
        private bool ProcessFile(FileInfo file, IndexDatabase db, bool log = false, string threadName = "Default thread")
        {
            IEnumerable<IVariableContainer<string, string>> metaData = null;

            this.Log<STATUS>(threadName + " is processing file <" + file.FullName + ">.");

            int crc;
            bool changedDocument;
            if (!GetCRC(file, out crc)) return false;
            string path = file.FullName;

            // doc already exists -> no changes, return. otherwise check if changes are present.
            try
            {
                if (db.DocumentExists(crc, path)) return true;
                changedDocument = db.DocumentExists(path);
            }
            catch (Exception ex) 
            {
                this.Log<ERROR>("Unable to check document status for <" + path + "> - ", ex);
                this.Log<ERROR>(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                    this.Log<ERROR>(ex.InnerException.StackTrace);
                }
                return false;
            }

            if (Global.InjectedResolverCount > 0) // use special resolvers
            {
                Document doc = GetOrCreateDocument(changedDocument, crc, path, db);
                if (doc == null) return false;

                bool resolved = ResolveDocument(doc, ref metaData);

                if (!resolved && !Settings.Default.UseDefaultResolversAsFallback) // do not use default resolvers
                {
                    if (log) this.Log<WARN>("<" + file.FullName + "> remains unresolved.");
                    return true;
                }

                #region add or update document

                Settings.Default.CurrentDocuments.Add(path);
                Settings.Default.Save();

                if (!AddOrUpdate(db, doc, changedDocument, file)) return false;
                if (!SubmitMetaData(db, file, doc, ref metaData)) return false;

                Settings.Default.CurrentDocuments.Remove(path);
                Settings.Default.Save();

                #endregion
            }
            else // use default resolvers
            {
                #region add or update document

                Settings.Default.CurrentDocuments.Add(path);
                Settings.Default.Save();

                if (changedDocument)
                {
                    Document doc = db.GetDocument(path);
                    doc.Checksum = crc;
                    try
                    {
                        db.UpdateDocument(doc);
                    }
                    catch (Exception ex)
                    {
                        this.Log<WARN>("<" + file.FullName + "> not processed. " + ex.Message);
                        this.Log<ERROR>(ex.StackTrace);
                        if (ex.InnerException != null)
                        {
                            this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                            this.Log<ERROR>(ex.InnerException.StackTrace);
                        }
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        db.AddDocument(crc, path, -1, null, true);
                    }
                    catch (Exception ex)
                    {
                        this.Log<WARN>("<" + file.FullName + "> not processed. " + ex.Message);
                        this.Log<ERROR>(ex.StackTrace);
                        if (ex.InnerException != null)
                        {
                            this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                            this.Log<ERROR>(ex.InnerException.StackTrace);
                        }
                        return false;
                    }
                }

                Settings.Default.CurrentDocuments.Remove(path);
                Settings.Default.Save();

                #endregion
            }

            if (log)
            {
                if (changedDocument)
                    this.Log<INFO>(threadName + " updated file <" + file.FullName + ">.");
                else
                    this.Log<INFO>(threadName + " indexed file <" + file.FullName + ">.");
            }
            
            return true;
        }

        private UpdateWorkerState GetStateWithMinWorkLoad(List<UpdateWorkerState> states) 
        {
            UpdateWorkerState min = states[0];
            foreach (UpdateWorkerState state in states)
                if (state.workLoadBytes < min.workLoadBytes) min = state;
            return min;
        }

        /// <summary>
        /// Prepare and start the index updating thread.
        /// </summary>
        /// <param name="useProgress"></param>
        private void StartMultiCoreIndexing(bool useProgress, bool useCallback, bool useLogging)
        {
            if (CurrentlyIndexing) return;
            CurrentlyIndexing = true;

            workers.Clear();
            startAnnounced = false;
            finishAnnounced = false;
            finishedFiles = 0;

            int numberOfThreads = Environment.ProcessorCount;
            if (Settings.Default.ThreadNumber != 0) 
                numberOfThreads = Settings.Default.ThreadNumber;
            else if (Settings.Default.MaxThreadNumber != 0)
                numberOfThreads = Math.Min(numberOfThreads, Settings.Default.MaxThreadNumber);

            List<UpdateWorkerState> states = new List<UpdateWorkerState>();
            for (int i = 0; i < numberOfThreads; i++) 
            {
                UpdateWorkerState state = new UpdateWorkerState();
                state.ThreadName = "Thread " + i.ToString();
                state.useServiceCallback = useCallback;
                state.useProgressDialog = useProgress;
                state.useLogging = useLogging;
                state.workLoad = new Queue<FileInfo>();
                state.indexDatabase = new IndexDatabase(Global.IndexDatabase.ConnectionString);
                state.indexDatabase.SetInitialized();

                if (useCallback) state.callback = OperationContext.Current.GetCallbackChannel<IUpdateCallback>();
                states.Add(state);
            }
            
            TotalSize = 0;
            NumberOfFilesToProcess = 0;

            (new Thread(new ParameterizedThreadStart(PrepareAndStartProcessing))).Start(states);
        }

        private void PrepareAndStartProcessing(object parameter)
        {
            List<UpdateWorkerState> states = parameter as List<UpdateWorkerState>;
            bool useProgress = states[0].useProgressDialog;

            #region select files

            foreach (string s in Settings.Default.DataPaths)
            {
                DirectoryInfo dir = new DirectoryInfo(s);
                
                IEnumerable<FileInfo> theFiles = null;

                try
                {
                    theFiles = dir.AllFiles();
                }
                catch (Exception ex)
                {
                    this.Log<ERROR>("Cannot access <" + dir.FullName + ">: ", ex);
                    CurrentlyIndexing = false;
                    return;
                }

                foreach (FileInfo fi in theFiles)
                {
                    if (Global.InjectedResolverCount != 0)
                    {
                        foreach (IDocumentResolver resolver in Global.InjectedResolvers)
                        {
                            if (resolver.CanResolve(new Document(fi)))
                            {
                                FilesToDo.Enqueue(fi);
                                break;
                            }
                        }
                    }
                    else FilesToDo.Enqueue(fi);
                }
            }

            #endregion
            #region count files

            SortFilesMultiCore();

            foreach (FileInfo file in FilesToDo)
            {
                NumberOfFilesToProcess++;
                TotalSize += (ulong)file.Length;

                UpdateWorkerState state = GetStateWithMinWorkLoad(states);
                state.workLoad.Enqueue(file);
                state.workLoadBytes += (ulong)file.Length;
            }

            FilesToDo.Clear();

            #endregion
            #region setup worker dialog

            if (useProgress)
            {
                if (os.IsAtLeastWindowsVista())
                {
                    #region create dialog

                    OperationsProgressDialog operationsProgressDialog = new OperationsProgressDialog();

                    operationsProgressDialog.CancelMessage = "Cancelling operation...";
                    // operationsProgressDialog.Action = OperationAction.Copying;
                    operationsProgressDialog.TotalCount = NumberOfFilesToProcess;
                    operationsProgressDialog.Message = "Updating the index.";
                    operationsProgressDialog.TargetPath = "Index Database";
                    operationsProgressDialog.SourcePath = "Index Source";
                    operationsProgressDialog.Caption = "Updating Index";
                    operationsProgressDialog.Title = "Updating Index";
                    operationsProgressDialog.DisplaysSourcePath = true;
                    operationsProgressDialog.DisplaysTargetPath = true;
                    operationsProgressDialog.ShowsTimeRemaining = true;
                    operationsProgressDialog.HasMinimizeButton = false;
                    operationsProgressDialog.HasCancelButton = true;
                    operationsProgressDialog.HasPauseButton = false;
                    operationsProgressDialog.HasProgressBar = true;
                    operationsProgressDialog.CompactsPaths = true;
                    operationsProgressDialog.AllowsUndo = false;
                    operationsProgressDialog.IsModal = true;
                    operationsProgressDialog.TotalSize = TotalSize;
                    operationsProgressDialog.TotalValue = 100;
                    operationsProgressDialog.Count = 0; //
                    operationsProgressDialog.Value = 0;
                    operationsProgressDialog.Size = 0; //

                    foreach (UpdateWorkerState state in states)
                        state.OperationsDialog = operationsProgressDialog;

                    #endregion
                }
                else
                {
                    #region create dialog

                    ProgressDialog progressDialog = new ProgressDialog();

                    progressDialog.ShowsTimeRemaining = true;
                    progressDialog.HasMinimizeButton = false;
                    progressDialog.HasCancelButton = true;
                    progressDialog.HasProgressBar = true;
                    progressDialog.CompactsPaths = true;
                    progressDialog.TotalValue = 100;
                    progressDialog.IsModal = true;
                    progressDialog.Value = 0;

                    foreach (UpdateWorkerState state in states)
                        state.Dialog = progressDialog;

                    #endregion
                }
            }

            #endregion

            RunMultiCoreUpdater(states);
        }

        /// <summary>
        /// Launch the index updater thread.
        /// </summary>
        /// <param name="userState"></param>
        private void RunMultiCoreUpdater(List<UpdateWorkerState> userStates)
        {
            if (userStates[0].useProgressDialog)
            {
                if (os.IsAtLeastWindowsVista()) userStates[0].OperationsDialog.ShowDialog();
                else userStates[0].Dialog.ShowDialog();
            }

            foreach (UpdateWorkerState state in userStates)
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerSupportsCancellation = true;
                worker.WorkerReportsProgress = state.useProgressDialog;

                worker.RunWorkerCompleted += UpdateThread_RunWorkerCompletedMultiCore;
                worker.DoWork += UpdateThread_DoWorkMultiCore;
                
                if (state.useProgressDialog || state.useServiceCallback)
                    worker.ProgressChanged += UpdateThread_ProgressChangedMultiCore;

                workers.Add(worker);
                worker.RunWorkerAsync(state);
            }
        }

        private void UpdateThread_DoWorkMultiCore(object sender, DoWorkEventArgs e)
        {
            int numberOfProblems = 0;

            BackgroundWorker worker = (BackgroundWorker)sender;
            UpdateWorkerState workerState = (UpdateWorkerState)e.Argument;

            lock (workerLock)
            {
                if (workerState.useServiceCallback && !startAnnounced)
                {
                    workerState.callback.NotifyStart(NumberOfFilesToProcess, TotalSize);
                    startAnnounced = true;
                }
            }

            this.Log<INFO>(workerState.ThreadName + " started.");

            while (workerState.workLoad.Count > 0)
            {
                FileInfo file = workerState.workLoad.Dequeue();
                if (worker.CancellationPending) break;

                workerState.currentItemSize = (ulong)file.Length;

                #region update progress

                try
                {
                    lock (workerLock)
                    {
                        int percentage = (int)((finishedFiles * 100ul) / NumberOfFilesToProcess);

                        if (workerState.useProgressDialog)
                            worker.ReportProgress(percentage, e.Argument);

                        if (workerState.useServiceCallback)
                            workerState.callback.ReportProgress(percentage, workerState.currentItemSize);

                        finishedFiles++;
                    }
                }
                catch (Exception ex) 
                {
                    this.Log<ERROR>("Unable to update the progress dialog: ", ex);
                    this.Log<ERROR>(ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                        this.Log<ERROR>(ex.InnerException.StackTrace);
                    }
                }

                #endregion

                if (!ProcessFile(file, workerState.indexDatabase, workerState.useLogging, workerState.ThreadName))
                {
                    numberOfProblems += 1;

                    #region cleanup

                    bool check = true;

                    try
                    {
                        if (Global.IndexDatabase.DocumentExists(file.FullName))
                            Global.IndexDatabase.GetDocument(file.FullName).Delete();
                        
                        numberOfProblems += 10;
                    }
                    catch (Exception ex)
                    {
                        check = false;

                        this.Log<ERROR>("Unable to delete partially processed document: ", ex);
                        this.Log<ERROR>(ex.StackTrace);
                        if (ex.InnerException != null)
                        {
                            this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                            this.Log<ERROR>(ex.InnerException.StackTrace);
                        }
                    }

                    try
                    {
                        Settings.Default.CurrentDocuments.Remove(file.FullName);
                        Settings.Default.Save();
                    }
                    catch (Exception ex)
                    {
                        check = false;

                        this.Log<ERROR>("Cleanup error: ", ex);
                        this.Log<ERROR>(ex.StackTrace);
                        if (ex.InnerException != null)
                        {
                            this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                            this.Log<ERROR>(ex.InnerException.StackTrace);
                        }
                    }

                    if (numberOfProblems > 5) break;
                    // if (check) workerState.workLoad.Enqueue(file); // TODO: retry only 3 times!

                    #endregion
                }
            }

            if (workerState.useLogging)
            {
                if (numberOfProblems == 0) this.Log<INFO>(workerState.ThreadName + " finished.");
                else this.Log<INFO>(workerState.ThreadName + " finished with errors.");
            }

            e.Result = e.Argument;
        }

        private void UpdateThread_ProgressChangedMultiCore(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                BackgroundWorker worker = (BackgroundWorker)sender;
                UpdateWorkerState workerState = (UpdateWorkerState)e.UserState;

                // Update progress bar
                if (os.IsAtLeastWindowsVista())
                {
                    workerState.OperationsDialog.Value = (ulong)e.ProgressPercentage;
                    workerState.OperationsDialog.Size += workerState.currentItemSize;
                    workerState.OperationsDialog.Count += 1;
                    // Cancel the operation if Cancel button has been pressed
                    if (workerState.OperationsDialog.IsCancelled) worker.CancelAsync();
                }
                else
                {
                    workerState.Dialog.Value = (ulong)e.ProgressPercentage;
                    // Cancel the operation if Cancel button has been pressed
                    if (workerState.Dialog.IsCancelled) worker.CancelAsync();
                }
            }
            catch (Exception ex)
            {
                this.Log<ERROR>("Unable to update the progress dialog: ", ex);
                this.Log<ERROR>(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                    this.Log<ERROR>(ex.InnerException.StackTrace);
                }
            }
        }

        private void UpdateThread_RunWorkerCompletedMultiCore(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker worker = null;
            UpdateWorkerState state = null;

            try
            {
                worker = (BackgroundWorker)sender; // not needed
                state = (UpdateWorkerState)e.Result;

                if (e.Error != null)
                {
                    this.Log<ERROR>("Worker thread error: ", e.Error);
                    this.Log<ERROR>(e.Error.StackTrace);
                    if (e.Error.InnerException != null)
                    {
                        this.Log<ERROR>("Inner Exception: ", e.Error.InnerException);
                        this.Log<ERROR>(e.Error.InnerException.StackTrace);
                    }
                }

                if (state.useProgressDialog)
                {
                    if (os.IsAtLeastWindowsVista()) state.OperationsDialog.CloseDialog();
                    else state.Dialog.CloseDialog();
                }
            }
            catch (Exception ex)
            {
                this.Log<ERROR>("Error closing progress dialog: ", ex);
                this.Log<ERROR>(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    this.Log<ERROR>("Inner Exception: ", ex.InnerException);
                    this.Log<ERROR>(ex.InnerException.StackTrace);
                }
            }
            finally 
            {
                lock (lockObject)
                {
                    worker = null;
                    CurrentlyIndexing = false;
                    if (state != null && state.useServiceCallback)
                    {
                        lock (workerLock)
                        {
                            if (!finishAnnounced)
                            {
                                state.callback.NotifyFinish(e.Cancelled);
                                finishAnnounced = true;
                            }
                        }
                    }
                }
            }
        }
        
        #endregion

        #endregion
        #region timer

        private void ServiceTimerCallback(object state)
        {
            lock (lockObject)
            {
                if (CurrentlyIndexing) return;
                
                if (IsIntervalInWholeDays)
                {
                    DateTime currentDateTime = DateTime.Now;
                    DateTime currentTime = new DateTime(1, 1, 1, currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second);
                    // DateTime lastUpdateTime = new DateTime(1, 1, 1, lastUpdateDateTime.Hour, lastUpdateDateTime.Minute, lastUpdateDateTime.Second);
                    DateTime lastCheckTime = new DateTime(1, 1, 1, lastCheckDateTime.Hour, lastCheckDateTime.Minute, lastCheckDateTime.Second);
                    
                    // was the time for updating in the interval between now and the last check?
                    if (lastCheckTime < Settings.Default.IndexUpdatingTime && currentTime > Settings.Default.IndexUpdatingTime)
                    {
                        // the time of day is right, now check if the last update is long enough in the past.
                        // subtract one hour from the interval to avoid postponing the moment further and further
                        TimeSpan minimumTimeToPass = currentDateTime.Subtract(lastUpdateDateTime).Subtract(new TimeSpan(1, 0, 0));
                        if (minimumTimeToPass < Settings.Default.IndexUpdatingInterval) // if this is still to small we are probably one or a few days to early.
                        {
                            lastCheckDateTime = DateTime.Now;
                            return; // but we have to wait at least another day.
                        }
                    }
                    else
                    {
                        // we are not in the preferred time window
                        lastCheckDateTime = DateTime.Now;
                        return;
                    }
                }

                Settings.Default.LastUpdate = DateTime.Now;
                Settings.Default.Save();
                StartIndexing(false, false, Global.TestMode);
            }
        }

        #endregion

        #endregion
    }
}