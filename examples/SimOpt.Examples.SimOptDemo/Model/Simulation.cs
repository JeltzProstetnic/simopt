using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using MatthiasToolbox.Simulation;
using MatthiasToolbox.Logging.Loggers;
using MatthiasToolbox.Simulation.Templates;
using MatthiasToolbox.Simulation.Enum;
using MatthiasToolbox.Simulation.Entities;

namespace MatthiasToolbox.SimOptExample.Model
{
    public class Simulation
    {
        #region cvar

        private Random rnd;
        public double startTime = new DateTime(2011, 1, 1).ToDouble();
        private int queueSize = 100;

        #endregion
        #region prop

        /// <summary>
        /// The model instance for this simulation experiment.
        /// </summary>
        public MatthiasToolbox.Simulation.Engine.Model Model { get; private set; }

        public SimpleBuffer Queue { get; private set; }
        public SimpleServer ServerA { get; private set; }
        public SimpleServer ServerB { get; private set; }
        public SimpleSink Sink { get; private set; }

        public int QueueSize
        {
            get { return queueSize; }
            set { queueSize = value; }
        }

        #endregion
        #region ctor

        public Simulation(RichTextBox log = null, bool logEvents = true, int seed = 123, int queueSize = 100)
        {
            this.QueueSize = queueSize;

            bool logging = log != null;
            if (logging) Simulator.RegisterSimulationLogger(new WPFRichTextBoxLogger(log));

            rnd = new Random(seed);

            Model = new MatthiasToolbox.Simulation.Engine.Model("SimOptDemo", seed, startTime);
            Model.LogEvents = logEvents;
            Model.LogStart = logging;
            Model.LogFinish = logging;

            BuildModel();
        }

        #endregion
        #region impl

        #region control

        /// <summary>
        /// Execute the simulation experiment.
        /// </summary>
        public void Run()
        {
            Model.Run();
        }
        
        /// <summary>
        /// Stop the simulation
        /// </summary>
        public void Stop()
        {
            Model.Stop();
            Model.Reset();
        }

        #endregion
        #region build

        public void BuildModel()
        {
            // the queue
            Queue = new SimpleBuffer(Model, QueueRule.FIFO, name: "TheQueue", maxCapacity: QueueSize);
            // Queue.BufferEmptyEvent.AddHandler(e => e.Model.Stop());

            // pre-fill with items
            FillQueue();
            
            // server A
            ServerA = new SimpleServer(Model, MachiningTimeFunctionA, name: "ServerA", autoStartDelay: 0,
                createProduct: CreateProductA);
            ServerA.AutoContinue = true;
            ServerA.ConnectTo(Queue);

            // server B
            ServerB = new SimpleServer(Model, MachiningTimeFunctionB, name: "ServerB", autoStartDelay: 0,
                createProduct: CreateProductB);
            ServerB.AutoContinue = true;
            ServerB.ConnectTo(Queue);
            
            // the sink
            Sink = new SimpleSink(Model, name: "TheSink", log: true);
            Sink.ConnectTo(ServerA);
            Sink.ConnectTo(ServerB);
            Sink.ItemReceived.AddHandler((s, e) => Model.RemoveEntity(e));
        }

        public void FillQueue(int n = 0)
        {
            int qs = n;
            if (n == 0) qs = QueueSize;

            Random rnd = new Random(Model.Seed.Value);
            for (int i = 0; i < qs; i++)
            {
                if (rnd.NextDouble() < 0.5)
                    Queue.Put(new SimpleEntity(Model, name: "A" + i.ToString(), id: "A" + i.ToString()));
                else
                    Queue.Put(new SimpleEntity(Model, name: "B" + i.ToString(), id: "B" + i.ToString()));
            }
        }

        #endregion
        #region optimize

        public IEnumerable<SimpleEntity> CreateCandidate(int queueSize = 0)
        {
            int i = 0;
            int qs = queueSize;
            if (queueSize == 0) qs = QueueSize;

            while (i < qs)
            {
                i++;
                 if (rnd.NextDouble() < 0.5)
                    yield return new SimpleEntity(Model, name: "A" + i.ToString(), id: "A" + i.ToString());
                else
                    yield return new SimpleEntity(Model, name: "B" + i.ToString(), id: "B" + i.ToString());
            }
        }

        public double Evaluate(IEnumerable<SimpleEntity> candidate)
        {
            if (!Model.IsReset) Model.Reset();
            
            FillQueue(candidate);
            
            Model.Run();

            return -(Model.CurrentTime - startTime);
        }

        #endregion

        #endregion
        #region util

        #region optimization

        private void FillQueue(IEnumerable<SimpleEntity> content)
        {
            foreach (SimpleEntity e in content)
            {
                Queue.Put(e);
            }
        }

        #endregion
        #region model execution

        /// <summary>
        /// function for calculating the machining time for an item
        /// </summary>
        /// <param name="currentMaterial"></param>
        /// <returns></returns>
        private double MachiningTimeFunctionA(List<SimpleEntity> currentMaterial)
        {
            if (currentMaterial == null || currentMaterial.Count == 0 || currentMaterial[0] == null) return double.NaN;

            if (currentMaterial[0].EntityName.StartsWith("A"))
                return new TimeSpan(0, 0, 2, 0, 0).ToDouble();
            else
                return new TimeSpan(0, 0, 9, 0, 0).ToDouble();
        }

        /// <summary>
        /// function for calculating the machining time for an item
        /// </summary>
        /// <param name="currentMaterial"></param>
        /// <returns></returns>
        private double MachiningTimeFunctionB(List<SimpleEntity> currentMaterial)
        {
            if (currentMaterial == null || currentMaterial.Count == 0 || currentMaterial[0] == null) return double.NaN;

            if (currentMaterial[0].EntityName.StartsWith("B"))
                return new TimeSpan(0, 0, 2, 0, 0).ToDouble();
            else
                return new TimeSpan(0, 0, 9, 0, 0).ToDouble();
        }

        private SimpleEntity CreateProductA(List<SimpleEntity> material)
        {
            Model.RemoveEntity(material[0]);
            return new SimpleEntity(Model, material[0].Identifier + "_A", material[0].EntityName + "_A");
        }

        private SimpleEntity CreateProductB(List<SimpleEntity> material)
        {
            Model.RemoveEntity(material[0]);
            return new SimpleEntity(Model, material[0].Identifier + "_B", material[0].EntityName + "_B");
        }

        #endregion

        #endregion
    }
}
