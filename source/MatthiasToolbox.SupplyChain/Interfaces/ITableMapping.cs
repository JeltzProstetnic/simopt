using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.SupplyChain.Interfaces
{
    /// <summary>
    /// interface for mapped tables with primary keys
    /// </summary>
    public interface ITableMapping
    {
        /// <summary>
        /// a unique integer id used as primary key
        /// </summary>
        int ID { get; }
    }
}
