using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Configuration.Install;
using System.Xml;
using System.IO;

namespace MatthiasToolbox.Utilities.Install
{
    /// <summary>
    /// This tool helps to transfer custom installer dialog data to the 
    /// applications settings and to change settings during the install process.
    /// </summary>
    public class InstallHelper
    {
        #region cvar

        private string installPath;

        private string assemblyPath;
        private string assemblyName;

        private Installer installer;
        private Configuration config;
        private InstallContext context;

        #endregion
        #region prop

        /// <summary>
        /// The path to the installation folder (usually in program files).
        /// </summary>
        public string InstallPath { get { return installPath; } }

        #endregion
        #region ctor

        /// <summary>
        /// Create an install helper. The installer context must be available.
        /// </summary>
        /// <param name="installer"></param>
        public InstallHelper(Installer installer)
        {
            this.installer = installer;
            this.context = installer.Context;
            this.assemblyPath = context.Parameters["assemblypath"];
            this.installPath = new FileInfo(assemblyPath).DirectoryName;
            this.config = ConfigurationManager.OpenExeConfiguration(assemblyPath);
            this.assemblyName = installer.GetType().Assembly.GetName().Name;
        }

        #endregion
        #region impl

        /// <summary>
        /// Save the changes to the config file to disk.
        /// </summary>
        public void Save()
        {
            config.Save(ConfigurationSaveMode.Modified);
        }

        #region change settings manually

        /// <summary>
        /// Overwrite a config setting.
        /// </summary>
        /// <param name="settingGroup"></param>
        /// <param name="settingName"></param>
        /// <param name="newData"></param>
        /// <param name="saveNow"></param>
        public void ChangeSetting(string settingGroup, string settingName, List<string> newData, bool saveNow = false)
        {
            string prefix = "<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\n";
            string postfix = "</ArrayOfString>";
            string open = "<string>";
            string close = "</string>\n";

            string result = prefix;
            foreach (string data in newData) result += open + data + close;
            result += postfix;

            ChangeSetting(settingGroup, settingName, result, saveNow);
        }

        /// <summary>
        /// Overwrite a user config setting.
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="newData"></param>
        /// <param name="saveNow"></param>
        public void ChangeUserSetting(string settingName, List<string> newData, bool saveNow = false)
        {
            ChangeSetting("userSettings", settingName, newData, saveNow);
        }

        /// <summary>
        /// Overwrite an application config setting.
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="newData"></param>
        /// <param name="saveNow"></param>
        public void ChangeApplicationSetting(string settingName, List<string> newData, bool saveNow = false)
        {
            ChangeSetting("applicationSettings", settingName, newData, saveNow);
        }

        /// <summary>
        /// Overwrite a config setting.
        /// </summary>
        /// <param name="settingGroup"></param>
        /// <param name="sourceParameter"></param>
        /// <param name="targetSetting"></param>
        /// <param name="saveNow"></param>
        public void ChangeSetting(string settingGroup, string settingName, string newValue, bool saveNow = false)
        {
            ConfigurationSectionGroup sectionGroup = config.GetSectionGroup(settingGroup);
            ClientSettingsSection section = sectionGroup.Sections[assemblyName + ".Properties.Settings"] as ClientSettingsSection;
            SettingElement settingElement = section.Settings.Get(settingName);

            // create new value node
            XmlDocument doc = new XmlDocument();
            XmlElement newValueElement = doc.CreateElement("value");
            newValueElement.InnerText = newValue;

            // change value
            settingElement.Value.ValueXml = newValueElement;

            // saving
            section.SectionInformation.ForceSave = true;
            if (saveNow) Save();
        }

        /// <summary>
        /// Overwrite a user config setting.
        /// </summary>
        /// <param name="sourceParameter"></param>
        /// <param name="targetSetting"></param>
        /// <param name="saveNow"></param>
        public void ChangeUserSetting(string settingName, string newValue, bool saveNow = false)
        {
            ChangeSetting("userSettings", settingName, newValue, saveNow);
        }

        /// <summary>
        /// Overwrite an application config setting.
        /// </summary>
        /// <param name="sourceParameter"></param>
        /// <param name="targetSetting"></param>
        /// <param name="saveNow"></param>
        public void ChangeApplicationSetting(string settingName, string newValue, bool saveNow = false)
        {
            ChangeSetting("applicationSettings", settingName, newValue, saveNow);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="names"></param>
        public void ChangeUserSettings(Dictionary<string, string> data, bool doSave = true)
        {
            foreach (KeyValuePair<string, string> kvp in data) ChangeUserSetting(kvp.Key, kvp.Value);
            if(doSave) Save();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="names"></param>
        public void ChangeApplicationSettings(Dictionary<string, string> data, bool doSave = true)
        {
            foreach (KeyValuePair<string, string> kvp in data) ChangeApplicationSetting(kvp.Key, kvp.Value);
            if (doSave) Save();
        }

        #endregion
        #region import settings from installer

        /// <summary>
        /// Overwrite a config setting with data from the installer context.
        /// </summary>
        /// <param name="settingGroup"></param>
        /// <param name="sourceParameter"></param>
        /// <param name="targetSetting"></param>
        /// <param name="saveNow"></param>
        public void ImportSetting(string settingGroup, string sourceParameter, string targetSetting = "", bool saveNow = false)
        {
            string target = string.IsNullOrEmpty(targetSetting) ? sourceParameter : targetSetting;

            ConfigurationSectionGroup sectionGroup = config.GetSectionGroup(settingGroup);
            ClientSettingsSection section = sectionGroup.Sections[assemblyName + ".Properties.Settings"] as ClientSettingsSection;
            SettingElement settingElement = section.Settings.Get(target);

            // create new value node
            XmlDocument doc = new XmlDocument();
            XmlElement newValue = doc.CreateElement("value");
            newValue.InnerText = context.Parameters[sourceParameter];

            // change value
            settingElement.Value.ValueXml = newValue;

            // saving
            section.SectionInformation.ForceSave = true;
            if (saveNow) Save();
        }

        /// <summary>
        /// Overwrite a user config setting with data from the installer context.
        /// </summary>
        /// <param name="sourceParameter"></param>
        /// <param name="targetSetting"></param>
        /// <param name="saveNow"></param>
        public void ImportUserSetting(string sourceParameter, string targetSetting = "", bool saveNow = false) 
        {
            ImportSetting("userSettings", sourceParameter, targetSetting, saveNow);
        }

        /// <summary>
        /// Overwrite an application config setting with data from the installer context.
        /// </summary>
        /// <param name="sourceParameter"></param>
        /// <param name="targetSetting"></param>
        /// <param name="saveNow"></param>
        public void ImportApplicationSetting(string sourceParameter, string targetSetting = "", bool saveNow = false) 
        {
            ImportSetting("applicationSettings", sourceParameter, targetSetting, saveNow);
        }

        /// <summary>
        /// Overwrite config settings with data from the installer context.
        /// This will save the settings to disk.
        /// </summary>
        /// <param name="data"></param>
        public void ImportSettings(params IInstallerParameterToConfigMapping[] data)
        {
            foreach (IInstallerParameterToConfigMapping mapping in data)
                ImportSetting(mapping.SettingGroupName, mapping.InstallerParameterName, mapping.SettingName);
            Save();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data">source parameter = key, target setting = value</param>
        /// <param name="doSave"></param>
        public void ImportUserSettings(Dictionary<string, string> data, bool doSave = true)
        {
            foreach (KeyValuePair<string, string> kvp in data) ImportUserSetting(kvp.Key, kvp.Value);
            if (doSave) Save();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data">source parameter = key, target setting = value</param>
        /// <param name="doSave"></param>
        public void ImportApplicationSettings(Dictionary<string, string> data, bool doSave = true)
        {
            foreach (KeyValuePair<string, string> kvp in data) ImportApplicationSetting(kvp.Key, kvp.Value);
            if (doSave) Save();
        }

        /// <summary>
        /// Overwrite user config settings with data from the installer context.
        /// This will save the settings to disk.
        /// </summary>
        /// <param name="names"></param>
        public void ImportUserSettings(params string[] names)
        {
            foreach (string name in names) ImportUserSetting(name);
            Save();
        }

        /// <summary>
        /// Overwrite application config settings with data from the installer context.
        /// This will save the settings to disk.
        /// </summary>
        /// <param name="names"></param>
        public void ImportApplicationSettings(params string[] names)
        {
            foreach (string name in names) ImportApplicationSetting(name);
            Save();
        }

        #endregion

        #endregion
    }
}