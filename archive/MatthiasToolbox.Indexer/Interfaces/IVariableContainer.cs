using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Indexer.Interfaces
{
    public interface IVariableContainer<TType, TObject> 
    {
        string Name { get; set; }
        TType DataType { get; set; }
        TObject Value { get; set; }
    }
}
