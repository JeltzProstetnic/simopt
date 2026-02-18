using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Basics.Datastructures.Geometry;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Basics.Datastructures.StateMachine;

namespace MatthiasToolbox.Simulation.Templates
{
    [Serializable]
    public class ConveyorInitializationParams : StateMachineEntityInitializationParams, IConveyorInitializationParams
    {
        public ConveyorInitializationParams(string id = "", 
                                            string name = "", 
                                            double vMax = 1,
                                            double acceleration = 1,
                                            double deceleration = 1,
                                            double length = 10,
                                            int numberOfSections = 10,
                                            double firstSectionOffset = 0,
                                            double positioningTolerance = 0.1,
                                            Vector orientation = null,
                                            Vector absoluteOffset = null,
                                            int? seedID = null, 
                                            Point initialPosition = null, 
                                            IResourceManager manager = null, 
                                            IEntity currentHolder = null,
                                            State initialState = null,
                                            IMovable mobileElement = null)
            : base(id, name, seedID, initialPosition, manager, currentHolder, initialState)
        {
            VMax = vMax;
            Acceleration = acceleration;
            Deceleration = deceleration;
            Length = length;
            NumberOfSections = numberOfSections;
            FirstSectionOffset = firstSectionOffset;
            Orientation = orientation;
            AbsoluteOffset = absoluteOffset;
            PositioningTolerance = positioningTolerance;
            MobileElement = mobileElement;
        }

        public ConveyorInitializationParams(string id = "",
                                            string name = "",
                                            double vMax = 1,
                                            double acceleration = 1,
                                            double deceleration = 1,
                                            double length = 10,
                                            int numberOfSections = 10,
                                            double firstSectionOffset = 0,
                                            double positioningTolerance = 0.1,
                                            Vector orientation = null,
                                            Vector absoluteOffset = null,
                                            int? seedID = null,
                                            Point initialPosition = null,
                                            IResourceManager manager = null,
                                            IEntity currentHolder = null,
                                            string initialState = null,
                                            IMovable mobileElement = null)
            : base(id, name, seedID, initialPosition, manager, currentHolder, initialState)
        {
            VMax = vMax;
            Acceleration = acceleration;
            Deceleration = deceleration;
            Length = length;
            NumberOfSections = numberOfSections;
            FirstSectionOffset = firstSectionOffset;
            Orientation = orientation;
            AbsoluteOffset = absoluteOffset;
            PositioningTolerance = positioningTolerance;
            MobileElement = mobileElement;
        }

        public ConveyorInitializationParams(string id = "",
                                            string name = "",
                                            double vMax = 1,
                                            double acceleration = 1,
                                            double deceleration = 1,
                                            double length = 10,
                                            int numberOfSections = 10,
                                            double firstSectionOffset = 0,
                                            double positioningTolerance = 0.1,
                                            Vector orientation = null,
                                            Vector absoluteOffset = null,
                                            int? seedID = null,
                                            Point initialPosition = null,
                                            IResourceManager manager = null,
                                            IEntity currentHolder = null,
                                            int? initialState = null,
                                            IMovable mobileElement = null)
            : base(id, name, seedID, initialPosition, manager, currentHolder, initialState)
        {
            VMax = vMax;
            Acceleration = acceleration;
            Deceleration = deceleration;
            Length = length;
            NumberOfSections = numberOfSections;
            FirstSectionOffset = firstSectionOffset;
            Orientation = orientation;
            AbsoluteOffset = absoluteOffset;
            PositioningTolerance = positioningTolerance;
            MobileElement = mobileElement;
        }


        #region IConveyorInitializationParams

        public double VMax { get; set; }

        public double Acceleration { get; set; }

        public double Deceleration { get; set; }

        public double Length { get; set; }

        public double PositioningTolerance { get; set; }

        public int NumberOfSections { get; set; }

        public double FirstSectionOffset { get; set; }

        public Vector Orientation { get; set; }

        public Vector AbsoluteOffset { get; set; }

        public IMovable MobileElement { get; set; }

        #endregion
    }
}
