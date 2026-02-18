using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Accord.Statistics.Filters;
using System.Data;

namespace DataProcessing
{
    public class FilterDescriptor
    {
        public IFilter Filter { get; set; }
        public string Name { get; set; }
        public DataTable Table { get; set; }
        public Type Form { get; set; }

        public FilterDescriptor(IFilter filter, string name, Type form)
        {
            Name = name;
            Filter = filter;
            Form = form;
        }
    }
}
