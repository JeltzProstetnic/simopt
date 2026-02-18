using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Network;
using MatthiasToolbox.Basics.Datastructures.Geometry;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Exceptions;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Simulation.Network
{
    [Serializable]
    public class Connection : ISimulationConnection
    {
        #region over

        public override string ToString()
        {
            string result = "[";
            if (!string.IsNullOrEmpty(Name)) result = Name + " [";
            result += "Connection " + instanceID.ToString() + "]";
            if (Node1 is Node)
            {
                result += " (" + (Node1 as Node).Name;
                if (IsDirected) result += " => "; else result += " <=> ";
                result += (Node2 as Node).Name + ")";
            }
            return result;
        }

        #endregion
        #region cvar

        private static int instanceCounter = 0;
        private int instanceID;

        private IEntity initialHolder;
        private IModel model;
        
        private bool modelSet;

        #endregion
        #region prop

        public bool IsDirected { get; set; }

        public bool IsTemporary { get; set; }

        public ISimulationNode Node1 { get; set; }

        public ISimulationNode Node2 { get; set; }

        public ISimulationConnection OriginalConnection { get; set; }

        public Tuple<INode<double>, INode<double>> ConnectedNodes 
        {
            get
            {
                return new Tuple<INode<double>, INode<double>>(Node1, Node2);
            }
        }

        public double Length { get; set; }

        public double Weight { get; set; }

        #region IResource

        public IEntity CurrentHolder { get; set; }

        public IResourceManager ResourceManager { get; internal set; }

        public bool Free { get; set; }

        #endregion
        #region IIdentifiable<string>

        public string Identifier { get; private set; }

        #endregion
        #region IEntity

        public string EntityName { get; set; }

        /// <summary>
        /// The simulation model which is associated with this entity.
        /// </summary>
        public IModel Model
        {
            get
            {
                return model;
            }
            set
            {
                if (modelSet) throw new ModelAlreadySetException();
                if (value != null)
                {
                    model = value;
                    modelSet = true;
                }
                else
                {
                    throw new NullReferenceException("The model reference may not be set to null.");
                }
            }
        }

        #endregion
        #region IEdge<Point>

        public string Name
        {
            get;
            set;
        }

        public IEnumerable<Point> Path
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IVertex<Point> Vertex1
        {
            get { return ConnectedNodes.Item1; }
            set { throw new NotImplementedException(); }
        }

        public IVertex<Point> Vertex2
        {
            get { return ConnectedNodes.Item2; }
            set { throw new NotImplementedException(); }
        }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// Create the connection and initialize resource if a resource manager is given.
        /// Caution: the network must be of type "MatthiasToolbox.Simulation.Tools.Network" for
        /// resource managing to work.
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <param name="length"></param>
        /// <param name="weight"></param>
        /// <param name="isDirected"></param>
        /// <param name="id"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public Connection(ISimulationNode node1, ISimulationNode node2, double length = 0, double weight = 0, bool isDirected = false, 
            string id = "", IResourceManager manager = null, IEntity currentHolder = null)
        {
            instanceID = instanceCounter++;

            Node1 = node1;
            Node2 = node2;
            Length = length;
            Weight = weight;
            IsDirected = isDirected;

            if (node1.Network is Network)
            {
                Model = (node1.Network as Network).Model;
                modelSet = true;
            }

            if(manager == null && Model != null) ResourceManager = Model.DefaultResourceManager;
            else ResourceManager = manager;
            if (ResourceManager != null) ResourceManager.Manage(this);
            if (currentHolder != null)
            {
                this.CurrentHolder = currentHolder;
                initialHolder = currentHolder;
                Free = false;
            }
            else
            {
                Free = true;
            }
            if (string.IsNullOrEmpty(id)) Identifier = ToString();
            else Identifier = id;
            EntityName = ToString();

            OriginalConnection = this;
        }

        #endregion
        #region impl

        /// <summary>
        /// compare connections by length
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(IConnection<double> other)
        {
            return this.Length.CompareTo(other.Length);
        }

        /// <summary>
        /// create and return a vector from the nodes in this connection
        /// direction will be from Node1 to Node2
        /// </summary>
        /// <returns></returns>
        public Vector GetVector()
        {
            if (!(Node1 is IPosition<Point>) || !(Node2 is IPosition<Point>)) 
                throw new ArgumentException("The nodes must implement ILocatable for a vector to be calculated!");
            return Vector.Create(Node1.Position, Node2.Position);
        }

        #region IResource

        public void Release()
        {
            Free = true;
            ResourceManager.Update();
        }

        #endregion
        #region IResettable

        public void Reset()
        {
            if (initialHolder != null)
            {
                CurrentHolder = initialHolder;
                Free = false;
            }
            else
            {
                Free = true;
            }
        }

        #endregion
        #region ICloneable

        public virtual object Clone()
        {
            Connection result = new Connection(Node1, Node2, Length, Weight, IsDirected, Identifier, ResourceManager, CurrentHolder);
            result.OriginalConnection = this.OriginalConnection;
            return result;
        }

        #endregion

        #endregion
        #region cast

        public static explicit operator Path(Connection connection)
        {
            return MatthiasToolbox.Simulation.Network.Path.Create(connection.Node1, connection.Node2);
        }

        #endregion
    }
}