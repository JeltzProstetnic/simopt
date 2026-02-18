using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Utilities.Install
{
    public interface IInstallerParameterToConfigMapping
    {
        string InstallerParameterName { get; }
        string SettingGroupName { get; }
        string SettingName { get; }
    }
}
