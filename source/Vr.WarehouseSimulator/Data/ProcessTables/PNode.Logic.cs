using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vr.WarehouseSimulator.Data.ProcessTables
{
    public partial class PNode
    {
        public override string ToString()
        {
            return NAME;
        }

        public List<PNode> GetProcessNodes(int parentID)
        {
            return PNode.GetPNodes(parentID);
        }
    }
}
