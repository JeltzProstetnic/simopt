using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Simulation.Interfaces
{
    public interface IElectiveBuffer<TItem, TIndex>
    {
        bool Put(TItem item, TIndex id);
        TItem Get(TIndex id);
        TItem Preview(TIndex id);
    }
}
