using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace DataProcessing
{
    public class DoubleRangeEditor : System.Drawing.Design.UITypeEditor
    {
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return System.Drawing.Design.UITypeEditorEditStyle.DropDown;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {

            return base.EditValue(context, provider, value);
        }
    }
}
