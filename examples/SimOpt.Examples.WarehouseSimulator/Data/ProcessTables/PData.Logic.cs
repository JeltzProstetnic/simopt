using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vr.WarehouseSimulator.Data.ProcessTables
{
    public partial class PData
    {
        #region over

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append("ID=\t\t\t");
            sb.Append(ID);
            
            sb.AppendLine();
            sb.Append("PNODEID=\t\t");    
            sb.Append(PNODEID);
            
            if (TARGET != 0)
            {
                sb.AppendLine();
                sb.Append("TARGET=\t\t");
                sb.Append(TARGET);
            }
            
            if (DELAY != 0)
            {
                sb.AppendLine();
                sb.Append("DELAY=\t\t\t");
                sb.Append(DELAY);
            }

            if (TARGETTYPEID != 0)
            {
                sb.AppendLine();
                sb.Append("TARGETTYPEID=\t\t");
                sb.Append(TARGETTYPEID);
            }

            if (TARGETINSTANCEID != 0)
            {
                sb.AppendLine();
                sb.Append("TARGETINSTANCEID=\t");
                sb.Append(TARGETINSTANCEID);
            }

            if (FRTYPEID != 0)
            {
                sb.AppendLine();
                sb.Append("FRTYPEID=\t\t");
                sb.Append(FRTYPEID);
            }

            if (FRINSTANCEID != 0)
            {
                sb.AppendLine();
                sb.Append("FRINSTANCEID=\t\t");
                sb.Append(FRINSTANCEID);
            }

            if (!string.IsNullOrEmpty(FRCLASSNAME))
            {
                sb.AppendLine();
                sb.Append("FRCLASSNAME=\t\t");
                sb.Append(FRCLASSNAME);
            }

            if (!string.IsNullOrEmpty(TARGETCLASSNAME))
            {
                sb.AppendLine();
                sb.Append("TARGETCLASSNAME=\t");
                sb.Append(TARGETCLASSNAME);
            }

            if (!string.IsNullOrEmpty(FRTYPE))
            {
                sb.AppendLine();
                sb.Append("FRTYPE=\t\t");
                sb.Append(FRTYPE);
            }

            if (!string.IsNullOrEmpty(TARGETTYPE))
            {
                sb.AppendLine();
                sb.Append("TARGETTYPE=\t");
                sb.Append(TARGETTYPE);
            }

            if (ITEMTYPEID != 0)
            {
                sb.AppendLine();
                sb.Append("ITEMTYPEID=\t\t");
                sb.Append(ITEMTYPEID);
            }

            if (COUNTERID != -1)
            {
                sb.AppendLine();
                sb.Append("COUNTERID=\t\t");
                sb.Append(COUNTERID);
            }

            if (!string.IsNullOrEmpty(STRATEGYNAME))
            {
                sb.AppendLine();
                sb.Append("STRATEGYNAME=\t");
                sb.Append(STRATEGYNAME);
            }

            return sb.ToString();
        }

        #endregion
    }
}
