using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Templates;
using System.Windows.Shapes;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Mathematics.Geometry;
using System.Windows.Media;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Geometry;
using MatthiasToolbox.Basics.Attributes;

namespace MatthiasToolbox.NetworkDriver.Model
{
    [Serializable]
    public class Conveyer : Conveyor<SimpleEntity>
    {
        [SimSerializationContainer]
        [NonSerialized]
        private Shape _shape;

        public Shape Shape
        {
            get { return _shape; }
            set { _shape = value; }
        }
        

       
        public decimal Value;
        
        //[NonSerializedAttribute]
        //public int MyProperty { get; set; }

        /// <summary>
        /// a wpf shape associated with this driver
        /// </summary>
      

        public Conveyer(IModel model, Point topLeft, Point bottomRight, string id)
            : base(model, id: id, name: id, length: bottomRight.X - topLeft.X - 20, numberOfSections: 6, firstSectionOffset: 5, initialPosition: topLeft, absoluteOffset: new Vector(0, 1))
        {
           


        }

        public Conveyer(IModel model, Point topLeft, Point bottomRight, Vector orientation,string id)
            : base(model,id:id,name:id, length: bottomRight.X - topLeft.X - 20, numberOfSections: 6, firstSectionOffset: 5, initialPosition: topLeft, absoluteOffset: new Vector(0, 1), orientation: orientation)
        {
        
        }
    }
}