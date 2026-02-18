using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Utilities.Install
{
    public class InstallerParameterToUserSettingsMapping : InstallerParameterToSettingsMapping, IInstallerParameterToConfigMapping
    {
        #region prop

        #region IInstallerParameterToConfigMapping

        public string InstallerParameterName { get; set; }

        public string SettingGroupName { get; set; }

        public string SettingName { get; set; }

        #endregion

        #endregion
        #region ctor

        public InstallerParameterToUserSettingsMapping(string sourceParameterName, string targetSettingName = "") 
            : base(sourceParameterName, targetSettingName)
        { }

        #endregion
    }
}