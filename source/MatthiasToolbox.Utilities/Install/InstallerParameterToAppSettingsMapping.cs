using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Utilities.Install
{
    public class InstallerParameterToAppSettingsMapping : InstallerParameterToSettingsMapping, IInstallerParameterToConfigMapping
    {
        #region prop

        #region IInstallerParameterToConfigMapping

        public string InstallerParameterName { get; set; }

        public string SettingGroupName { get; set; }

        public string SettingName { get; set; }

        #endregion

        #endregion
        #region ctor

        public InstallerParameterToAppSettingsMapping(string sourceParameterName, string targetSettingName = "") 
            : base(sourceParameterName, targetSettingName, "applicationSettings")
        { }

        #endregion
    }
}