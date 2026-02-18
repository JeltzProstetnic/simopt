using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using MatthiasToolbox.Indexer.Properties;
using MatthiasToolbox.Utilities;
using MatthiasToolbox.Utilities.Install;
using Microsoft.Win32;

namespace MatthiasToolbox.Indexer.Service
{
    [RunInstaller(true)]
    public partial class IndexUpdateServiceInstaller : Installer
    {
        #region cvar

        private InstallHelper helper;

        // general settings
        public static string name = Settings.Default.ServiceName;
        public static string installPath = Settings.Default.InstallPath;
        public static string displayName = Settings.Default.ServiceDisplayName;
        public static string plugin = Settings.Default.DocumentResolverPluginName;
        public static ServiceStartMode startMode = Settings.Default.ServiceStartMode;
        public static ServiceAccount accountType = Settings.Default.ServiceAccountType;
        public static string controllerApplication = Settings.Default.ControllerApplication;
        public static bool useDefaultResolversAsFallback = Settings.Default.UseDefaultResolversAsFallback;

        // index updating
        public static TimeSpan indexUpdatingInterval = new TimeSpan(1, 0, 0, 0); // one day
        public static DateTime indexUpdatingTime = new DateTime(1, 1, 1, 1, 0, 0); // 01:00 AM

        #endregion
        #region ctor

        /// <summary>
        /// Default Constructor for WindowsServiceInstaller.
        /// </summary>
        public IndexUpdateServiceInstaller()
        {
//#if DEBUG
//            Debugger.Launch();
//#endif

            ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();

            // Service Account Information 
            serviceProcessInstaller.Account = accountType;
            
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

            // Service Information
            serviceInstaller.DisplayName = displayName;
            serviceInstaller.StartType = startMode;

            // This must be identical to the MatthiasToolbox.Indexer.IndexUpdateService.ServiceBase name
            serviceInstaller.ServiceName = name;

            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(serviceInstaller);
        }

        #endregion
        #region impl

        /// <summary>
        /// requires the following variables from the installer app:
        ///     IndexConnectionString   string
        /// </summary>
        /// <param name="stateSaver"></param>
        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            // allow gui interaction for service
            RegistryKey ckey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name, true);
            if (ckey != null)
            {
                if (ckey.GetValue("Type") != null)
                {
                    ckey.SetValue("Type", ((int)ckey.GetValue("Type") | 256));
                }
            }

            helper = new InstallHelper(this);
            
            // import from installer
            helper.ImportUserSetting("IndexConnectionString");
            
            // change with static values
            helper.ChangeUserSetting("ServiceName", name);
            helper.ChangeUserSetting("InstallPath", installPath);
            helper.ChangeUserSetting("ServiceDisplayName", displayName);
            helper.ChangeUserSetting("DocumentResolverPluginName", plugin);
            helper.ChangeUserSetting("ServiceStartMode", startMode.ToString());
            helper.ChangeUserSetting("LastUpdate", DateTime.MinValue.ToString());
            helper.ChangeUserSetting("ServiceAccountType", accountType.ToString());
            helper.ChangeUserSetting("ControllerApplication", controllerApplication);
            helper.ChangeUserSetting("IndexUpdatingTime", indexUpdatingTime.ToString());
            helper.ChangeUserSetting("IndexUpdatingInterval", indexUpdatingInterval.ToString());
            helper.ChangeUserSetting("UseDefaultResolversAsFallback", useDefaultResolversAsFallback.ToString());
            
            helper.Save();
        }

        /// <summary>
        /// Nothing special to do yet.
        /// </summary>
        /// <param name="savedState"></param>
        public override void Uninstall(IDictionary savedState)
        {
            try
            {
                base.Uninstall(savedState);
            }
            finally
            {
                try
                {
                    SystemTools.ExecuteShellCommand("sc delete " + name);
                }
                catch { }
            }
        }

        #endregion
    }
}