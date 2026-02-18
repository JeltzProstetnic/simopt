using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Entities;
using System.Windows.Forms.DataVisualization.Charting;
using MatthiasToolbox.Simulation.Events;
using MatthiasToolbox.SupplyChain.Simulator;
using System.ComponentModel;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    public partial class Site : StochasticEntity, INotifyPropertyChanged
    {
        private int stock;
        private int patientCount;
        private Depot supplyDepot;

        public int PatientCount
        {
            get { return patientCount; }
            set
            {
                patientCount = value;
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("PatientCount"));
            }
        }

        public int Stock
        {
            get { return stock; }
            set
            {
                stock = value;
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Stock"));
            }
        }

        public Depot SupplyDepot
        {
            get
            {
                if (supplyDepot == null)
                {
                    var q = from row in Global.ModelDatabase.DepotTable where row.ID == DepotID select row;
                    if (q.Any()) supplyDepot = q.First();
                }
                return supplyDepot;
            }
        }

        public Series RecruitingChartSeries { get; set; }

        public readonly BinaryEvent<Site, Patient> PatientRecruited = new BinaryEvent<Site, Patient>("PatientRecruited");

        public int Patients;

        public bool TakeMedication()
        {
            Stock -= 1;
            // if (stock < 4) SupplyDepot.ScheduleResupplyEvent(this);
            return true;
        }

        public override void OnReset()
        {
            base.OnReset();
            Stock = InitialStock;
        }


        #region INotifyPropertyChanged Member

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
