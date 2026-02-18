using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.SupplyChain.Database.ModelTables;
using MatthiasToolbox.Simulation.Events;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.SupplyChain.Simulator
{
    public class Patient : StochasticEntity // must be added to model to act as a seed source
    {
        public int n;

        private Site homeSite;

        public readonly BinaryEvent<Patient, Site> VisitEvent;

        private readonly GaussianDistribution VisitDistribution;

        private Random<double> rndVisit;

        public Patient(double start, Site homeSite) : base(homeSite.Model)
        {
            n = 4;
            this.homeSite = homeSite;
            VisitDistribution = new GaussianDistribution(new TimeSpan(3, 0, 0, 0).Ticks, new TimeSpan(1, 0, 0, 0).Ticks);
            rndVisit = new Random<double>(homeSite, VisitDistribution, Model.Antithetic, Model.NonStochasticMode);
            VisitEvent = new BinaryEvent<Patient, Site>(EntityName + ".Visit");
            VisitEvent.AddHandler(InternalVisitHandler);
            VisitEvent.Log = Model.LoggingEnabled;
            double r = rndVisit.Next();
            double t = start + r;
            Model.AddEvent(t, VisitEvent.GetInstance(this, homeSite));
        }

        public void InternalVisitHandler(Patient sender, Site site)
        {
            n--;
            if (n > 0) Model.AddEvent(rndVisit.Next(), VisitEvent.GetInstance(this, site));
            homeSite.TakeMedication();
        }
    }
}