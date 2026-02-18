using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Entities;
using System.Windows.Shapes;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Mathematics.Geometry;
using System.Windows.Media;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Basics.Attributes;

namespace MatthiasToolbox.NetworkDriver.Model
{
    [Serializable]
    public class Item : SimpleEntity
    {
        /// <summary>
        /// a wpf shape associated with this driver
        /// </summary>
        /// 
        [NonSerialized]
        private Shape _shape;

        public Shape Shape
        {
            get { return _shape; }
            set { _shape = value; }
        }
      

        public Item(IModel model) : base(model)
        {
        
        }

        public override bool Load(MatthiasToolbox.Simulation.Tools.ModelState state)
        {
            bool result = base.Load(state);
  
            return result;
        }

    
    }
}