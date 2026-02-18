using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MatthiasToolbox.Logging.Loggers;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;
using MatthiasToolbox.Simulation;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Enum;
using MatthiasToolbox.Simulation.Templates;

namespace MatthiasToolbox.SQSSModel
{
    /// <summary>
    /// The Source-Queue-Server-Sink model is considered a showcase model for simulation 
    /// software. It is one of the simplest possible models in which still non-linear 
    /// effects typical for discrete simulation may occur.
    /// 
    /// The model consists of a SimpleSource instance which creates entities in normal 
    /// distributed time intervals using a Generator function. The source is connected to
    /// a Queue (alternatively a delay can be put in between them). The queue is a 
    /// SimpleBuffer with a FIFO rule and a capacity for 15 items.
    /// 
    /// The next item is a server, which pulls items from the queue in constant time 
    /// intervals. The server passes on the finished items to the last simulation object 
    /// in the chain which is the sink. The sink does nothing except for counting the 
    /// items it receives.
    /// </summary>
    [Serializable]
    public class Simulation
    {
        #region cvar

        private int productCounter = 0;

        private GaussianDistribution gauss;
        private ConstantDoubleDistribution c0nst;

        private DateTime startTime ;
        private TimeSpan sourceIntervalAverage = new TimeSpan(0, 0, 2, 0, 0);
        private TimeSpan sourceIntervalDeviation = new TimeSpan(0, 0, 0, 10, 0);
        private TimeSpan sourceStartDelay = new TimeSpan(0, 0, 1, 30, 0);

        #endregion
        #region prop

        public Model Model { get; private set; }

        public SimpleSource Source { get; private set; }
        public SimpleBuffer Queue { get; private set; }
        // public SimpleDelay delay;
        public SimpleServer Server { get; private set; }
        public SimpleSink Sink { get; private set; }

        public SimpleEntity Product { get; private set; }

        #endregion
        #region ctor

        /// <summary>
        /// Create the SQSS Model
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="logTextBox"></param>
        public Simulation(int seed, RichTextBox logTextBox) 
        {
            startTime = new DateTime(1900, 1, 1, 0, 0, 0,(int) 0);
            Model = new Model("SQSS", seed, startTime);
            Model.LogStart = true;
            Model.LogFinish = true;
            Simulator.RegisterSimulationLogger(new RichTextBoxLogger(logTextBox));
        }

        /// <summary>
        /// Create the SQSS Model
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="logTextBox"></param>
        public Simulation(int seed)
        {
            startTime = DateTime.MinValue;
            Model = new Model("SQSS", seed, startTime);
            Model.LogStart = true;
            Model.LogFinish = true;
       }


        #endregion
        #region hand

        private void ProductFinished(IEntity sender, SimpleEntity product)
        {
            if (Model.LoggingEnabled) 
                sender.Log<SIM_INFO>(product.ToString() + " finished.", Model);
        }

        private void SourceHandler(IEntity sender, SimpleEntity entity)
        {
            // if (productCounter > 20) source.Stop();
            // this.Log<SIM_INFO>("Queue has " + queue.Count.ToString() + " items.", model);
        }

        private void ItemReceived(IEntity sender, SimpleEntity item)
        {
            if (Queue.Count > 1 && Server.Idle) Server.Start();
        }

        private void QueueRejectedItem(SimpleEntity entity)
        {
            if (Model.LoggingEnabled) 
                this.Log<SIM_WARNING>(entity.EntityName + 
                    " was not accepted because the queue is full.", Model);
        }

        private void QueueFull(IEntity entity)
        {
            if (Model.LoggingEnabled) this.Log<SIM_WARNING>("Queue just ran full!", Model);
        }

        #endregion
        #region impl

        /// <summary>
        /// Initializes all entities and builds the model.
        /// </summary>
        public void BuildModel()
        {
            

            // distributions
            gauss = new GaussianDistribution(sourceIntervalAverage.ToDouble(), 
                sourceIntervalDeviation.ToDouble());
            c0nst = new ConstantDoubleDistribution(sourceIntervalAverage.ToDouble() * 4, 
                false);


            Product = new SimpleEntity(Model, "Product1", "Product1");
            
            // the source
            Source = new SimpleSource(Model, gauss, ProductGenerator, name: "TheSource");
           
            Source.EntityCreatedEvent.AddHandler(SourceHandler);
            Source.EntityCreatedEvent.Log = true;

            //// the delay
            //delay = new SimpleDelay(model, c0nst, name: "TheDelay");
            //delay.LogReceive = true;
            //delay.LogRelease = true;
            //delay.LogReject = true;
            //delay.ConnectTo(source);

            // the queue
            Queue = new SimpleBuffer(Model, QueueRule.FIFO, 
                name: "TheQueue", maxCapacity: 15);
            Queue.ItemReceivedEvent.AddHandler(ItemReceived);
            Queue.BufferFullEvent.AddHandler(QueueFull);
            Queue.NotifyItemNotAccepted = QueueRejectedItem;
            //Queue.ConnectTo(Source);

       
            // the server
            Server = new SimpleServer(Model, c0nst, name: "TheServer", 
                checkMaterialUsable: CheckMaterialUsable, 
                checkMaterialComplete: CheckMaterialComplete);
            Server.EntityFinishedEvent.AddHandler(ProductFinished);
            Server.AutoContinue = true;
            // server.PushAllowed = true;
            // server.ConnectTo(source);
            Server.ConnectTo(Queue);

            // the sink
            Sink = new SimpleSink(Model, name: "TheSink", log: true);
            Sink.ConnectTo(Server);
        }

        /// <summary>
        /// Start the simulation.
        /// </summary>
        /// <param name="hoursToRun"></param>
        public void Start(int hoursToRun) 
        {
            Source.Start(sourceStartDelay);
            Model.Run(startTime.AddHours(hoursToRun));
        }

        /// <summary>
        /// Continue simulation
        /// </summary>
        /// <param name="hoursToRun"></param>
        public void Continue(int hoursToRun)
        {
            Source.Start();
            Model.Run(Model.CurrentDateTime.AddHours(hoursToRun));
        }

        /// <summary>
        /// Stop the simulation.
        /// </summary>
        public void Stop()
        {
            Model.Stop();
        }

        public SimpleEntity ProductGenerator()
        {
            productCounter += 1;
            return new SimpleEntity(Model, "Product " + productCounter.ToString(), 
                "Product " + productCounter.ToString());
       }

        private bool CheckMaterialUsable(SimpleEntity material)
        {
            bool result = Server.CurrentMaterial.Count < 2;
            if (!result && Model.LoggingEnabled) 
                Server.Log<SIM_INFO>(material.ToString() + " was discarded.", Model);
            return result;
        }

        private bool CheckMaterialComplete(List<SimpleEntity> material)
        {
            return material.Count == 2;
        }

        #endregion
    }
}