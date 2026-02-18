using System.Windows.Shapes;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Network;
using MatthiasToolbox.Simulation.Interfaces;
using System;
using MatthiasToolbox.Basics.Attributes;

namespace MatthiasToolbox.NetworkDriver.Model
{
    /// <summary>
    /// a very simple mobile entity
    /// </summary>
    [Serializable]
    public class Driver : MobileEntity
    {
        /// <summary>
        /// a wpf shape associated with this driver
        /// </summary>
        [NonSerialized]
        private Shape _shape;

        public Shape Shape
        {
            get { return _shape; }
            set { _shape = value; }
        }

        /// <summary>
        /// default ctor, just calls base
        /// </summary>
        /// <param name="model"></param>
        /// <param name="id"></param>
        /// <param name="startNode"></param>
        /// <param name="vMax"></param>
        /// <param name="acceleration"></param>
        /// <param name="deceleration"></param>
        public Driver(IModel model, string id, Node startNode, double vMax, double acceleration, double deceleration)
            : base(model, id, id, startNode, vMax, acceleration, deceleration)
        {
        }
    }
}