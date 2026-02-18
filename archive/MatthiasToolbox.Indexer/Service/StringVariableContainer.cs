using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Indexer.Interfaces;

namespace MatthiasToolbox.Indexer.Service
{
    public class StringVariableContainer : IVariableContainer<string, string>
    {
        #region IVariableContainer<string,string>

        public string Name { get; set; }

        public string DataType { get; set; }

        public string Value { get; set; }

        #endregion

        public StringVariableContainer(string name, string value) 
        {
            this.Name = name;
            this.Value = value;
            DataType = "string";
        }
    }
}
