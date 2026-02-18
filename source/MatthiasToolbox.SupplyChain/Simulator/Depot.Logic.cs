using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Entities;
using System.ComponentModel;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    public partial class Depot : StochasticEntity, INotifyPropertyChanged
    {
        private int stock;
        public int Stock
        {
            get { return stock; }
            set
            {
                stock = value;
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Stock"));
            }
        }

        #region INotifyPropertyChanged Member

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
