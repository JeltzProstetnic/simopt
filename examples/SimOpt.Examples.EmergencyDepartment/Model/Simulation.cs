using SimOpt.Examples.EmergencyDepartment.Interfaces;
using System;
using System.Collections.Generic;
using SimOpt.Simulation;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Events;
using SimOpt.Simulation.Engine;

namespace SimOpt.Examples.EmergencyDepartment.Model
{
    /// <summary>
    /// Description of Simulation.
    /// </summary>
    public class Simulation
    {
        #region cvar

        int task;

        public UnaryEvent<List<ITreatmentPoint<IPatient>>> startWorking;
        private Dictionary<String, ITreatmentRoom<IPatient>> destinations;

        #endregion
        #region prop

        public IModel Model { get; set; }

        #endregion
        #region ctor

        public Simulation(int seed, int task)
        {
            Model = new SimOpt.Simulation.Engine.Model("Emergency Department Simulation", seed, DateTime.Today.AddHours(7.5).ToDouble());

            startWorking = new UnaryEvent<List<ITreatmentPoint<IPatient>>>(Model.Name + "StartWorking");
            startWorking.Log = true;
            startWorking.AddHandler(StartWorkingHandler);
            this.task = task;
            destinations = new Dictionary<String, ITreatmentRoom<IPatient>>();

            switch (task)
            {
                case 1:
                    BuildTaskA();
                    break;
                case 2:
                    BuildTaskB();
                    break;
                case 3:
                    BuildTaskC();
                    break;
            }
        }

        #endregion
        #region impl

        private void BuildTaskA()
        {
            PatientSource source = new PatientSource(Model, GetNextPatient);

            ITreatmentPoint<IPatient> reg = new TreatmentPoint(Model, "Registration", 0.2, 0.5, 1.0);
            TreatmentRoom registration = new TreatmentRoom(Model, "Registration Room", new List<ITreatmentPoint<IPatient>> { reg });

            ITreatmentPoint<IPatient> doc1 = new TreatmentPoint(Model, "Doctor1", true, 1.5, 3.2, 5.0);
            ITreatmentPoint<IPatient> doc2 = new TreatmentPoint(Model, "Doctor2", true, 1.5, 3.2, 5.0);
            TreatmentRoom cw1 = new TreatmentRoom(Model, "Casuality Ward 1", new List<ITreatmentPoint<IPatient>> { doc1, doc2 });

            ITreatmentPoint<IPatient> doc3 = new TreatmentPoint(Model, "Doctor3", true, 2.8, 4.1, 6.3);
            ITreatmentPoint<IPatient> doc4 = new TreatmentPoint(Model, "Doctor4", true, 2.8, 4.1, 6.3);
            TreatmentRoom cw2 = new TreatmentRoom(Model, "Casuality Ward 2", new List<ITreatmentPoint<IPatient>> { doc3, doc4 });

            Model.AddEventAt(Model.CurrentTime.ToDateTime().AddHours(0.5).ToDouble(), startWorking.GetInstance(new List<ITreatmentPoint<IPatient>> { doc1, doc2, doc3, doc4 }));

            ITreatmentPoint<IPatient> xray1 = new TreatmentPoint(Model, "x-Ray 1", 2.0, 2.8, 4.1);
            ITreatmentPoint<IPatient> xray2 = new TreatmentPoint(Model, "x-Ray 2", 2.0, 2.8, 4.1);
            TreatmentRoom xrayRoom = new TreatmentRoom(Model, "X-Ray Room", new List<ITreatmentPoint<IPatient>> { xray1, xray2 });

            ITreatmentPoint<IPatient> plaster = new TreatmentPoint(Model, "Plaster", 3.0, 3.8, 4.7);
            TreatmentRoom plasterRoom = new TreatmentRoom(Model, "Plaster Room", new List<ITreatmentPoint<IPatient>> { plaster });

            Exit exit = new Exit(Model, "Exit");

            destinations["registration"] = registration;
            destinations["cw1"] = cw1;
            destinations["cw2"] = cw2;
            destinations["plaster"] = plasterRoom;
            destinations["xray"] = xrayRoom;
            destinations["exit"] = exit;
            source.Destinations = destinations;
        }

        private void BuildTaskB()
        {
            PatientSource source = new PatientSource(Model, GetNextPatient);

            ITreatmentPoint<IPatient> reg = new TreatmentPoint(Model, "Registration", 0.2, 0.5, 1.0);
            TreatmentRoom registration = new TreatmentRoom(Model, "Registration Room", new List<ITreatmentPoint<IPatient>> { reg });

            ITreatmentPoint<IPatient> doc1 = new CWTreatmentPoint(Model, "Doctor1", true, 1.5, 3.2, 5.0);
            ITreatmentPoint<IPatient> doc2 = new CWTreatmentPoint(Model, "Doctor2", true, 1.5, 3.2, 5.0);
            CWTreatmentRoom cw1 = new CWTreatmentRoom(Model, "Casuality Ward 1", new List<ITreatmentPoint<IPatient>> { doc1, doc2 });

            ITreatmentPoint<IPatient> doc3 = new CWTreatmentPoint(Model, "Doctor3", true, 2.8, 4.1, 6.3);
            ITreatmentPoint<IPatient> doc4 = new CWTreatmentPoint(Model, "Doctor4", true, 2.8, 4.1, 6.3);
            CWTreatmentRoom cw2 = new CWTreatmentRoom(Model, "Casuality Ward 2", new List<ITreatmentPoint<IPatient>> { doc3, doc4 });

            ((CWTreatmentPoint)doc1).NewRoom = cw2;
            ((CWTreatmentPoint)doc1).CheckRoom = cw2;
            ((CWTreatmentPoint)doc2).NewRoom = cw2;
            ((CWTreatmentPoint)doc2).CheckRoom = cw2;
            ((CWTreatmentPoint)doc3).NewRoom = cw1;
            ((CWTreatmentPoint)doc3).CheckRoom = cw2;
            ((CWTreatmentPoint)doc4).NewRoom = cw1;
            ((CWTreatmentPoint)doc4).CheckRoom = cw2;

            Model.AddEventAt(Model.CurrentTime.ToDateTime().AddHours(0.5).ToDouble(), startWorking.GetInstance(new List<ITreatmentPoint<IPatient>> { doc1, doc2, doc3, doc4 }));

            ITreatmentPoint<IPatient> xray1 = new TreatmentPoint(Model, "x-Ray 1", 2.0, 2.8, 4.1);
            ITreatmentPoint<IPatient> xray2 = new TreatmentPoint(Model, "x-Ray 2", 2.0, 2.8, 4.1);
            TreatmentRoom xrayRoom = new TreatmentRoom(Model, "X-Ray Room", new List<ITreatmentPoint<IPatient>> { xray1, xray2 });

            ITreatmentPoint<IPatient> plaster = new TreatmentPoint(Model, "Plaster", 3.0, 3.8, 4.7);
            TreatmentRoom plasterRoom = new TreatmentRoom(Model, "Plaster Room", new List<ITreatmentPoint<IPatient>> { plaster });

            Exit exit = new Exit(Model, "Exit");

            destinations["registration"] = registration;
            destinations["cw1"] = cw1;
            destinations["cw2"] = cw2;
            destinations["plaster"] = plasterRoom;
            destinations["xray"] = xrayRoom;
            destinations["exit"] = exit;
            source.Destinations = destinations;
        }

        private void BuildTaskC()
        {
            PatientSource source = new PatientSource(Model, GetNextPatient);

            ITreatmentPoint<IPatient> reg = new TreatmentPoint(Model, "Registration", 0.2, 0.5, 1.0);
            TreatmentRoom registration = new TreatmentRoom(Model, "Registration Room", new List<ITreatmentPoint<IPatient>> { reg });

            ITreatmentPoint<IPatient> doc1 = new TreatmentPoint(Model, "Doctor1", true, 1.5, 3.2, 5.0);
            ITreatmentPoint<IPatient> doc2 = new TreatmentPoint(Model, "Doctor2", true, 1.5, 3.2, 5.0);
            TreatmentRoom cw1 = new TreatmentRoom(Model, "Casuality Ward 1", new List<ITreatmentPoint<IPatient>> { doc1, doc2 }, true);

            ITreatmentPoint<IPatient> doc3 = new TreatmentPoint(Model, "Doctor3", true, 2.8, 4.1, 6.3);
            ITreatmentPoint<IPatient> doc4 = new TreatmentPoint(Model, "Doctor4", true, 2.8, 4.1, 6.3);
            TreatmentRoom cw2 = new TreatmentRoom(Model, "Casuality Ward 2", new List<ITreatmentPoint<IPatient>> { doc3, doc4 }, true);

            Model.AddEventAt(Model.CurrentTime.ToDateTime().AddHours(0.5).ToDouble(), startWorking.GetInstance(new List<ITreatmentPoint<IPatient>> { doc1, doc2, doc3, doc4 }));

            ITreatmentPoint<IPatient> xray1 = new TreatmentPoint(Model, "x-Ray 1", 2.0, 2.8, 4.1);
            ITreatmentPoint<IPatient> xray2 = new TreatmentPoint(Model, "x-Ray 2", 2.0, 2.8, 4.1);
            TreatmentRoom xrayRoom = new TreatmentRoom(Model, "X-Ray Room", new List<ITreatmentPoint<IPatient>> { xray1, xray2 }, true);

            ITreatmentPoint<IPatient> plaster = new TreatmentPoint(Model, "Plaster", 3.0, 3.8, 4.7);
            TreatmentRoom plasterRoom = new TreatmentRoom(Model, "Plaster Room", new List<ITreatmentPoint<IPatient>> { plaster });

            Exit exit = new Exit(Model, "Exit");

            destinations["registration"] = registration;
            destinations["cw1"] = cw1;
            destinations["cw2"] = cw2;
            destinations["plaster"] = plasterRoom;
            destinations["xray"] = xrayRoom;
            destinations["exit"] = exit;
            source.Destinations = destinations;
        }

        public void StartWorkingHandler(List<ITreatmentPoint<IPatient>> data)
        {
            foreach (ITreatmentPoint<IPatient> treat in data)
            {
                treat.StartTreatment();
            }
        }

        IPatient GetNextPatient(PatientSource source)
        {
            return new Patient(Model, source);
        }

        #endregion
    }
}
