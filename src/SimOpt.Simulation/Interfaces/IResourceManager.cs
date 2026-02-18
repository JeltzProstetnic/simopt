using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Engine;
using System.Runtime.Serialization;

namespace SimOpt.Simulation.Interfaces
{

    /// <summary>
    /// Interface for a simulation resource manager.
    /// </summary>
    /// <remarks>rc</remarks>
    public interface IResourceManager : IResettable //, ISerializableGrubi
    {
        #region prop

        /// <summary>
        /// Indicates if reservations with higher priorities can 
        /// "steal" resources which are already booked for
        /// reservations with lower priority.
        /// </summary>
        bool StealingAllowed { get; }

        /// <summary>
        /// all resources managed by this instance
        /// </summary>
        IEnumerable<IResource> ManagedResources { get; }

        /// <summary>
        /// the currently available resources
        /// </summary>
        IEnumerable<IResource> FreeResources { get; }

        /// <summary>
        /// the bookings currently placed on this instance
        /// </summary>
        IEnumerable<IResourceReservation> Reservations { get; }

        #endregion
        #region init

        /// <summary>
        /// Initialize the resource manager
        /// </summary>
        void Initialize();

        #endregion
        #region impl

        #region seizing

        /// <summary>
        /// submit a reservation for one item of a given type
        /// </summary>
        /// <typeparam name="TReceiver"></typeparam>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="forObject"></param>
        /// <param name="pickupMethod"></param>
        void Seize<TResource>(IEntity forObject, 
                              Func<TResource, bool> pickupMethod, 
                              Func<TResource, bool> checkMethod, 
                              Priority priority)
            where TResource : IResource;

        /// <summary>
        /// submit a reservation for one item of a given type
        /// </summary>
        /// <typeparam name="TReceiver"></typeparam>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="forObject"></param>
        /// <param name="pickupMethod"></param>
        void Seize<TResource>(IEntity forObject,
                              Func<TResource, bool> pickupMethod,
                              Func<TResource, bool> checkMethod)
            where TResource : IResource;

        /// <summary>
        /// submit a reservation for a number of items of a given type
        /// </summary>
        /// <typeparam name="TReceiver"></typeparam>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="nrOfRequiredItems"></param>
        /// <param name="forObject">the holder for the objects, once they have been acquired</param>
        /// <param name="pickupMethod"></param>
        void Seize<TResource>(int nrOfRequiredItems,
                              IEntity forObject, 
                              Func<List<TResource>, bool> pickupMethod,
                              Func<TResource, bool> checkMethod, 
                              Priority priority) 
            where TResource : IResource;

        /// <summary>
        /// submit a reservation for one item each of the given types
        /// </summary>
        /// <typeparam name="T">type of the intended receiver</typeparam>
        /// <param name="requiredResources">list of required types</param>
        /// <param name="forObject">the holder for the objects, once they have been acquired</param>
        /// <param name="pickupMethod"></param>
        void Seize(Type[] requiredResources,
                   IEntity forObject,
                   ResourcesReadyDelegate pickupMethod,
                   Dictionary<Type, Func<IResource, bool>> checkMethods,
                   Priority priority);

        /// <summary>
        /// submit a reservation for the given types and the number of required objects for each type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="requiredResources">list of required types; the second generic parameter is the number of resources of Type you need</param>
        /// <param name="forObject">the holder for the objects, once they have been acquired</param>
        /// <param name="pickupMethod"></param>
        void Seize(Dictionary<Type, int> requiredResources,
                   IEntity forObject, 
                   ResourcesReadyDelegate pickupMethod,
                   Dictionary<Type, Func<IResource, bool>> checkMethods, 
                   Priority priority = null);

        #endregion
        #region management

        /// <summary>
        /// Tell the resource manager that he is responsible for this resouce
        /// </summary>
        /// <param name="resource"></param>
        void Manage(IResource resource);

        /// <summary>
        /// Tell the resource manager that he is responsible for these resouces
        /// </summary>
        /// <param name="resource"></param>
        void Manage(IEnumerable<IResource> resources);

        /// <summary>
        /// Tell the resource manager that he is no longer responsible for this resouce
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="force">
        /// If set to true, the given resource will be removed (stolen) 
        /// from an active reservation if it is currently booked.
        /// </param>
        void UnManage(IResource resource, bool force);

        /// <summary>
        /// Tell the resource manager that he is no longer responsible for these resouces
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="force">
        /// If set to true, the given resources will be removed (stolen) 
        /// from an active reservation if they are currently booked.
        /// </param>
        void UnManage(IEnumerable<IResource> resources, bool force);

        #endregion

        /// <summary>
        /// Update all reservations. This has to be called whenever 
        /// a resource is released or a new reservation is placed.
        /// A manual call is usually unnecessary.
        /// </summary>
        void Update();

        #endregion
    }
}