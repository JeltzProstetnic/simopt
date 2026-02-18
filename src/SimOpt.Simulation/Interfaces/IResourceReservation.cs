using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation.Interfaces
{
    /// <summary>
    /// A resource reservation for one or many resources, which
    /// are to be delivered simultaneously.
    /// </summary>
    /// <remarks>beta</remarks>
    public interface IResourceReservation
    {
        #region prop

        /// <summary>
        /// Return true as soon as all ordered
        /// items are available.
        /// </summary>
        bool Finished { get; }

        /// <summary>
        /// The entity waiting for the booked items, think of it as a "customer"
        /// </summary>
        IEntity Orderer { get; }

        /// <summary>
        /// The resources which are already available. Caution, these can be 
        /// reduced again before the reservation is finished and delivered by
        /// a reservation with higher priority.
        /// </summary>
        IEnumerable<IResource> AllAvailableItems { get; }

        /// <summary>
        /// The resources which are already available. Caution, these can be 
        /// reduced again before the reservation is finished and delivered by
        /// a reservation with higher priority.
        /// </summary>
        Dictionary<Type, List<IResource>> AvailableItems { get; }

        /// <summary>
        /// The resources which are not yet available.
        /// </summary>
        Dictionary<Type, int> MissingItems { get; }

        /// <summary>
        /// The resources which were ordered.
        /// </summary>
        Dictionary<Type, int> RequiredItems { get; }

        /// <summary>
        /// The method to pick up the resource(s) when 
        /// it(they) are ready. The method returns false 
        /// if the resources are not being used after all.
        /// </summary>
        ResourcesReadyDelegate PickupMethod { get; }

        /// <summary>
        /// The methods used to determine if a certain resource 
        /// can actually be used for this reservation. This may
        /// be dependent on circumstances besides the type alone.
        /// </summary>
        Dictionary<Type, Func<IResource, bool>> CheckAcceptanceMethods { get; }

        #endregion
        #region impl

        /// <summary>
        /// Provide a resource to be used for this reservation.
        /// </summary>
        /// <typeparam name="T">An IResource to use for this reservation</typeparam>
        /// <param name="resource">The resource to use for this reservation</param>
        void Put<T>(T resource) where T : IResource;

        /// <summary>
        /// Provide a resource to be used for this reservation.
        /// </summary>
        /// <param name="resource">The resource to use for this reservation</param>
        void Put(IResource resource);

        /// <summary>
        /// "Steal" a resource from this reservation.
        /// </summary>
        /// <param name="resource">The resource to "steal".</param>
        /// <returns>
        /// The resource which was originally booked
        /// for this reservation. May also return 
        /// null if stealing the resource was not 
        /// possible for some reason. (e.g. the 
        /// actual resource manager doesn't support 
        /// stealing)
        /// </returns>
        IResource Steal(IResource resource);

        #endregion
    }
}
