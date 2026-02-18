using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Semantics.Interfaces;

namespace MatthiasToolbox.Semantics.Metamodel
{
    public partial class Instance : ILINQTable, ISemanticNode
    {
        #region IPosition<Point>

        public System.Windows.Point Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion
        #region ITitle

        public string Title
        {
            get
            {
                string result = Get<string>("Title");
                
                if (String.IsNullOrEmpty(result)) return Name;
                else return result;
            }
            set
            {
                if (HasProperty("Title")) Set<string>("Title", value, true);
                else Name = value;
            }
        }

        #endregion

        public System.Windows.Size Size
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public System.Windows.Media.Color BackgroundColor
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public System.Windows.Media.Color ForegroundColor
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}