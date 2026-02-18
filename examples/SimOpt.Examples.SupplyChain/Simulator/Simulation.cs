using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.SupplyChain.Database;
using MatthiasToolbox.SupplyChain.Database.ModelTables;
using MatthiasToolbox.Simulation.Events;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;
using MatthiasToolbox.Presentation;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Simulation.Entities;

namespace MatthiasToolbox.SupplyChain.Simulator
{
    public class Simulation
    {
        #region cvar

        private MainWindow win;

        // counter
        private int day = 0;

        // model + components
        private ModelDatabase currentData;
        private List<Site> sites;
        private List<Depot> depots;

        #endregion
        #region dele

        public delegate void RecruitingUpdateDelegate(Site site, int dayInTrial);

        #endregion
        #region evnt

        public event RecruitingUpdateDelegate RecruitingUpdated;

        #endregion
        #region prop

        public IModel SupplyChainModel { get; private set; }

        #endregion
        #region ctor

        public Simulation(int seed, DateTime startDateTime, MainWindow win, string name = "Unnamed Supply Chain Model")
        {
            SupplyChainModel = new Model(name, seed, startDateTime);
            SupplyChainModel.LoggingEnabled = true;
            sites = new List<Site>();
            depots = new List<Depot>();
            this.win = win;
        }

        #endregion
        #region init

        public bool BuildModel(ModelDatabase modelData, int scenarioID = 1)
        {
            sites.Clear();
            depots.Clear();
            currentData = modelData;

            foreach (Depot depot in modelData.DepotTable)
            {
                // if (!depot.Initialized) // new depot
                    depot.Initialize(SupplyChainModel, new StochasticEntityInitializationParams("Depot " + depot.ID.ToString()));
                if (!SupplyChainModel.HasEntity(depot.Identifier)) // new model
                    SupplyChainModel.AddEntity(depot);
            }

            foreach (Site site in modelData.SiteTable)
            {
                // if (!site.Initialized) // new depot // has to be re-initialized because it is drawing seeds
                site.Initialize(SupplyChainModel, new StochasticEntityInitializationParams("Site " + site.ID.ToString()));
                if (!SupplyChainModel.HasEntity(site.Identifier)) // new model
                    SupplyChainModel.AddEntity(site);

                site.PatientRecruited.Handlers.Clear(); // adding the recruiting handler is not seen as adding the handler again because sim is a new instance every time
                site.PatientRecruited.AddHandler(RecruitingHandler);

                sites.Add(site);
            }

            return true;
        }

        #endregion
        #region impl

        public bool StartSimulation(bool? logEvents = false)
        {
            SupplyChainModel.LoggingEnabled = logEvents.Value;

            Random<double> rand = new Random<double>(SupplyChainModel, new ErlangDistribution(1, 1));
            double start;
            day = 0;

            foreach (Site site in sites)
            {
                site.Patients = 0;
                site.PatientCount = 0;
                site.Stock = 100;
                site.PatientRecruited.Log = logEvents.Value;
                for (int i = 0; i < 30; i++)
                {
                    double r = Math.Abs(rand.Next());
                    start = r * (double)(new TimeSpan(21, 0, 0, 0).Ticks) // + SupplyChainModel.StartTime
                        + (double)(new TimeSpan(site.StartDelay, 0, 0, 0).Ticks);
                    SupplyChainModel.AddEvent(start, site.PatientRecruited.GetInstance(site, new Patient(start, site)));
                }
            }

            ((Model)SupplyChainModel).Schedule(new TimeSpan(5, 0, 0, 0), Repeater);

            SupplyChainModel.Start();
            return true;
        }

        public void Repeater()
        {
            if (day < 150) ((Model)SupplyChainModel).Schedule(new TimeSpan(5, 0, 0, 0), Repeater);
            day += 5;

            foreach (Site site in sites)
            {
                RecruitingUpdated.Invoke(site, day);
                site.Patients = 0;
            }
            
            win.DoEvents();
        }

        public void RecruitingHandler(Site site, Patient patient)
        {
            site.Patients += 1;
            site.PatientCount += 1;
        }

        #endregion
        #region util


        #endregion
    }
}