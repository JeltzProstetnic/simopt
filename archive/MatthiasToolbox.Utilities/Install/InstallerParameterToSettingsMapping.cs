using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Utilities.Install
{
    public class InstallerParameterToSettingsMapping : IInstallerParameterToConfigMapping
    {
        #region prop

        #region IInstallerParameterToConfigMapping

        public string InstallerParameterName { get; set; }

        public string SettingGroupName { get; set; }

        public string SettingName { get; set; }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// If no settingName is given, it is assumed to be equal to the sourceParameter name.
        /// </summary>
        /// <param name="sourceParameter"></param>
        /// <param name="settingName"></param>
        /// <param name="settingGroup"></param>
        public InstallerParameterToSettingsMapping(string sourceParameterName, string targetSettingName = "", string settingGroup = "userSettings")
        {
            this.InstallerParameterName = sourceParameterName;
            this.SettingGroupName = settingGroup;
            this.SettingName = targetSettingName;
        }

        #endregion
    }
}
