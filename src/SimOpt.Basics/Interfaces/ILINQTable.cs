using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace SimOpt.Basics.Interfaces
{
    /// <summary>
    /// Empty interface for a LINQ to SQL mapping class.
    /// </summary>
    public interface ILINQTable : IIdentifiable<int>, INotifyPropertyChanging
    {
    }
}
