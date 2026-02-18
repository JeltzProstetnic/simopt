using System;
using System.Collections.Generic;
using System.Linq;

using MatthiasToolbox.Mathematics.Graphing.Algorithms;
using MatthiasToolbox.Mathematics.Stochastics;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Network;
using MatthiasToolbox.Simulation.Tools;
using MatthiasToolbox.Mathematics.Geometry;
using System.Windows.Shapes;
using MatthiasToolbox.Simulation.Templates;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Geometry;

namespace MatthiasToolbox.NetworkDriver.Model
{
    [Serializable]
    public class Simulation
    {
        #region cvar

        private Random rnd;
        private Action syncAction;
        private Node firstSpecialNode;
        private Node lastSpecialNode;

        #endregion
        #region prop

        public MatthiasToolbox.Simulation.Engine.Model Model { get; private set; }

        public Network Network { get; private set; }

        public List<Node> Nodes { get; private set; }

        public IEnumerable<Connection> Connections 
        {
            get { return Network.Connections.Cast<Connection>(); }
        }

        public List<Driver> Drivers { get; private set; }

        public Driver SpecialDriver { get; private set; }

        public Conveyer TestConveyor1 { get; set; }

        public Conveyer TestConveyor2 { get; set; }

        public List<Item> TestItems1 { get; set; }

        public List<Item> TestItems2 { get; set; }

        #endregion
        #region ctor

        public Simulation(int seed = 14, 
            int nrOfNodes = 50, 
            double longDistanceConnectivity = 0.5d, 
            int nrOfDrivers = 5, 
            int maxNeighboursToConnect = 2,
            double shortDistanceConnectivity = 0.31d)
        {
            CreateModel(seed);
            CreateNetwork(nrOfNodes, longDistanceConnectivity, nrOfDrivers, maxNeighboursToConnect, shortDistanceConnectivity);
            CreateDrivers(nrOfDrivers);
            CreateConveyors();
        }

        #region Model Factory

        private void CreateModel(int seed)
        {
            this.rnd = new Random(seed);

            this.Model = new MatthiasToolbox.Simulation.Engine.Model("Network Driver Demo Model", seed, 0);
            this.Model.LogEvents = true;
        }

        private void CreateNetwork(int nrOfNodes = 50,
            double longDistanceConnectivity = 0.5d,
            int nrOfDrivers = 5,
            int maxNeighboursToConnect = 2,
            double shortDistanceConnectivity = 0.31d)
        {
            this.Network = new Network(Model, "The Network", "The Network", new Dijkstra(), true);

            for (int i = 0; i <= nrOfNodes; i++)
            {
                string id = "Node " + i.ToString();
                Node n = new Node(id, id, Network, rnd.NextDouble(),
                    new Point(rnd.NextDouble() * 750 + 50, rnd.NextDouble() * 550 + 50));
                n.DistanceDelegate = DistanceDelegate;
            }

            this.Nodes = Network.Nodes.Cast<Node>().ToList();

            CreateRandomConnections(longDistanceConnectivity, shortDistanceConnectivity, maxNeighboursToConnect);

            firstSpecialNode = new Node("Special 1", "Special 1", Network, 1, new Point(10, 10));
            Node n1 = new Node("Special 2", "Special 2", Network, 1, new Point(110, 10));
            Node n2 = new Node("Special 3", "Special 3", Network, 1, new Point(210, 10));
            lastSpecialNode = new Node("Special 4", "Special 4", Network, 1, new Point(310, 10));

            Nodes.AddRange(new Node[] { firstSpecialNode, n1, n2, lastSpecialNode });

            firstSpecialNode.ConnectTo(n1);
            n1.ConnectTo(n2);
            n2.ConnectTo(lastSpecialNode);
        }

        private void CreateRandomConnections(double longDistanceConnectivity = 0.5d, 
            double shortDistanceConnectivity = 0.31d, 
            int maxNeighboursToConnect = 2)
        {
            for (int i = 1; i <= maxNeighboursToConnect; i++)
            {
                if (rnd.NextDouble() > shortDistanceConnectivity) continue;

                foreach (Node n in Nodes)
                {
                    Node nearestNode = null;
                    double dmin = double.PositiveInfinity;
                    foreach (Node n2 in Nodes)
                    {
                        if (n == n2) continue;
                        double d = n.Position.GetEuclideanDistance(n2.Position);
                        if (d < dmin && !n.IsConnectedTo(n2))
                        {
                            dmin = d;
                            nearestNode = n2;
                        }
                    }

                    if (nearestNode != null)
                    {
                        // just a convenience hack: usually DistanceTo would not work here because the nodes aren't connected yet.
                        n.ConnectTo(nearestNode, n.DistanceTo(nearestNode), 1, rnd.NextDouble() > 0.75 ? true : false);
                    }
                }
            }

            foreach (Node n in Nodes)
            {
                if (rnd.NextDouble() > 1 - longDistanceConnectivity)
                {
                    Node other = Nodes.RandomItem(rnd);
                    while (other == n || other.IsConnectedTo(n) || n.IsConnectedTo(other)) other = Nodes.RandomItem(rnd);

                    // just a convenience hack: usually DistanceTo would not work here because the nodes aren't connected yet.
                    n.ConnectTo(other, n.DistanceTo(other), 1, rnd.NextDouble() > 0.75 ? true : false);
                }
            }
        }

        private void CreateDrivers(int nrOfDrivers = 5)
        {
            this.Drivers = new List<Driver>();
            for (int i = 0; i < nrOfDrivers; i++)
            {
                Driver d = new Driver(Model, "Driver " + i.ToString(), Nodes.RandomItem(rnd), 5, 1, 2);
                d.ArrivalEvent.AddHandler(OnDriverArrived);
                this.Drivers.Add(d);
            }

            SpecialDriver = new Driver(Model, "Special Driver", firstSpecialNode, 7d, 0.05d, 0.05d);
            SpecialDriver.PassThroughEvent.Log = true;
            SpecialDriver.ArrivalEvent.Log = true;
            SpecialDriver.ArrivalEvent.AddHandler(ArrivalHandler);
            this.Drivers.Add(SpecialDriver);

            SpecialDriver.InitializeTaskMachine();
            SpecialDriver.TaskMachine.AddTask(new DriveTask(SpecialDriver, lastSpecialNode));
        }

        private void CreateConveyors()
        {
            TestConveyor1 = new Conveyer(Model, new Point(40, 20), new Point(240, 30), new Vector(1, 0),"Conv1");
            int n = 10;
            TestItems1 = new List<Item>() { new Item(Model), new Item(Model), new Item(Model), new Item(Model) };
            foreach (Item i in TestItems1)
            {
                TestConveyor1.Put(i, new Point(n, 0));
                n += 30;
            }
            TestConveyor1.AutoDetach = true;

            TestConveyor2 = new Conveyer(Model, new Point(40, 40), new Point(240, 50),"Conv2");
            TestConveyor2.StoppedEvent.AddHandler(ConveyorStopped);
            n = 0;
            TestItems2 = new List<Item>() { new Item(Model), new Item(Model), new Item(Model), new Item(Model) };
            foreach (Item i in TestItems2)
            {
                TestConveyor2.Put(i, n);
                n += 1;
            }
        }

        int n = 0;
        private void ConveyorStopped(IMovable sender, Point position)
        {
            if (TestConveyor2.HasItemAtLastPosition)
            {
                SimpleEntity item = TestConveyor2.GetItemAtLastPosition();
                TestConveyor2.DetachItem(item);
                item.Position = new Point(item.Position.X, item.Position.Y + n * 8);
                n++;
            }
        }

        #endregion

        #endregion
        #region init

        public void InitializeDrivers()
        {
            foreach (Driver d in Drivers)
            {
                if (d == SpecialDriver) continue; // the special driver is controlled by a task machine
                List<Node> connectedNodes = d.CurrentNode.ConnectedNodes.Cast<Node>().ToList();
                if (connectedNodes.Count > 0)
                {
                    Node target = connectedNodes.RandomItem(rnd);
                    d.DriveTo(target);
                }
            }

            SpecialDriver.TaskMachine.StartTaskSequence();

            TestConveyor1.Advance(30d);
            TestConveyor2.AdvanceToEnd();
        }

        #endregion
        #region hand

        private void OnDriverArrived(IEntity sender, ISimulationNode target)
        {
            Node newTarget;
            Driver d = sender as Driver;
            List<Node> connectedNodes = d.CurrentNode.ConnectedNodes.Cast<Node>().ToList();
            if (connectedNodes.Count > 0)
            {
                newTarget = connectedNodes.RandomItem(rnd);
                d.DriveTo(newTarget);
            }
            else
            {
                newTarget = Nodes.RandomItem(rnd);
                while (newTarget == d.CurrentNode) 
                    newTarget = Nodes.RandomItem(rnd);
                d.DriveTo(newTarget);
            }
        }

        private void ArrivalHandler(MobileEntity entity, ISimulationNode node)
        {
            if (node == lastSpecialNode)
            {
                entity.DriveTo(firstSpecialNode);
            }
            else
            {
                entity.DriveTo(lastSpecialNode);
            }

            TestConveyor1.Advance(30d);
            TestConveyor2.Advance();
        }

        #endregion
        #region impl

        public AsyncModelRunner StartAsync(Action synchronize, double speed)
        {
            this.syncAction = synchronize; 
            
            AsyncModelRunner amr = new AsyncModelRunner(Model);
            amr.PreferredSpeed = speed;
            amr.SynchronizationIntervalMS = 50;
            amr.Synchronize += new AsyncModelRunner.ModelStepDelegate(amr_Synchronize);
            amr.StartAsync();
            return amr;
        }

        private void amr_Synchronize(AsyncModelRunner sender, IModel model)
        {
            syncAction.Invoke();
        }

        #endregion
        #region util

        /// <summary>
        /// just a convenience hack: this will ignore connection weights if the nodes are not connected.
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <returns></returns>
        private double DistanceDelegate(ISimulationNode node1, ISimulationNode node2)
        {
            if (!node1.SimulationConnections.ContainsKey(node2)) return node1.Position.GetEuclideanDistance(node2.Position);
            return node1.SimulationConnections[node2].Length * node1.SimulationConnections[node2].Weight;
        }

        #endregion
    }
}