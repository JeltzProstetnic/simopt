using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Controls;

namespace FinanceManager.Tools
{
    public interface ITreeViewItem : INotifyPropertyChanged
    {
        string Name { get; }
        bool IsSelected { get; set; }
        IEnumerable<ITreeViewItem> Children { get; }
    }
}
