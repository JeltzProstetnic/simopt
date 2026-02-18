using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.SupplyChain.Interfaces
{
    public interface IDataExtension : ITableMapping
    {
        string TableName { get; }
    }
}
