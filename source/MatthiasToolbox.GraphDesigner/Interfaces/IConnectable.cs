using System.Collections.Generic;
using System.ComponentModel;
using MatthiasToolbox.GraphDesigner.Utilities;

namespace MatthiasToolbox.GraphDesigner.Interfaces
{
    public interface IConnectable : INotifyPropertyChanged
    {
        List<object> Connections { get; }
        ConnectionInfo GetInfo();
    }
}
