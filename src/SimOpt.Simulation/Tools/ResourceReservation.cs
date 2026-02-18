using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation.Tools
{
    /// <summary>
    /// A straight forward implementation of 
    /// IResourceReservation with stealing enabled.
    /// </summary>
    /// <remarks>beta</remarks>
    public class ResourceReservation : IResourceReservation
    {
        #region cvar

        // the remaining total number of missing resources
        private int finishedCounter = 0;

        #endregion
        #region prop

        /// <summary>
        /// Will return true as soon as all ordered
        /// items are available.
        /// </summary>
        public bool Finished { get { return finishedCounter == 0; } }

        /// <summary>
        /// The initiator of the reservation which will 
        /// recieve the resource(s) when it/they are/is ready
        /// </summary>
        public IEntity Orderer { get; private set; }

        /// <summary>
        /// Resources which are already reserved for this instance.
        /// Depending on the ResourceManager and its handling of
        /// priorities, items may be taken away from this list.
        /// </summary>
        public IEnumerable<IResource> AllAvailableItems
        {
            get
            {
                foreach (Type t in AvailableItems.Keys)
                {
                    foreach (IResource res in AvailableItems[t])
                        yield return res;
                }
            }
        }

        /// <summary>
        /// Resources which are already reserved for this instance.
        /// Depending on the ResourceManager and its handling of
        /// priorities, items may be taken away from this list.
        /// </summary>
        public Dictionary<Type, List<IResource>> AvailableItems { get; private set; }

        /// <summary>
        /// Resources which are yet missing from this instance.
        /// </summary>
        public Dictionary<Type, int> MissingItems { get; private set; }

        /// <summary>
        /// All needed types for this reservation with the
        /// required number of instances.
        /// </summary>
        public Dictionary<Type, int> RequiredItems { get; private set; }
        
        /// <summary>
        /// The method which will pick up the resources
        /// as soon as they are ready to use.
        /// </summary>
        public ResourcesReadyDelegate PickupMethod { get; private set; }

        /// <summary>
        /// For each required type the method which will
        /// tell if the presented instance is acceptable.
        /// </summary>
        public Dictionary<Type, Func<IResource, bool>> CheckAcceptanceMethods { get; private set; } 

        #endregion
        #region ctor

        /// <summary>
        /// Create a resource reservation
        /// </summary>
        /// <param name="orderer">
        /// The entity for which to book the given items. 
        /// This will be the holder of the resources after
        /// the order is delivered (if they are actually used)
        /// </param>
        /// <param name="orderedResources">
        /// The types and numbers of items to book
        /// </param>
        /// <param name="pickupMethod">
        /// The method to recieve the resources when they are ready
        /// </param>
        /// <param name="checkMethods">
        /// The method(s) which will decide if a given resource is 
        /// acceptable for this reservation.
        /// </param>
        public ResourceReservation(IEntity orderer,
            Dictionary<Type, int> orderedResources,
            ResourcesReadyDelegate pickupMethod,
            Dictionary<Type, Func<IResource, bool>> checkMethods) 
        {
            this.Orderer = orderer;
            this.RequiredItems = orderedResources;
            this.PickupMethod = pickupMethod;
            this.CheckAcceptanceMethods = checkMethods;

            MissingItems = new Dictionary<Type, int>(orderedResources);
            AvailableItems = new Dictionary<Type, List<IResource>>();
            foreach (Type t in orderedResources.Keys)
            {
                finishedCounter += orderedResources[t];
                AvailableItems[t] = new List<IResource>();
            }
        }

        #endregion
        #region impl

        /// <summary>
        /// Provide a resource to be used for this reservation.
        /// Caution: will throw an exception if a resource of the
        /// type T is not needed.
        /// </summary>
        /// <typeparam name="T">An IResource to use for this reservation</typeparam>
        /// <param name="resource">The resource to use for this reservation</param>
        public void Put<T>(T resource) where T : IResource
        {
            Type t = typeof(T);
            if (MissingItems[t] <= 0) throw new ArgumentException("No more resources of the type \"" + t.FullName + "\" were required.", "resource");
            MissingItems[t] -= 1;
            finishedCounter -= 1;
            AvailableItems[t].Add(resource);
        }

        /// <summary>
        /// Provide a resource to be used for this reservation.
        /// Caution: will throw an exception if a resource of the
        /// type T is not needed.
        /// </summary>
        /// <param name="resource">The resource to use for this reservation</param>
        public void Put(IResource resource)
        {
            Type t = resource.GetType();
            if (MissingItems[t] <= 0) throw new ArgumentException("No more resources of the type \"" + t.FullName + "\" were required.", "resource");
            MissingItems[t] -= 1;
            finishedCounter -= 1;
            AvailableItems[t].Add(resource);
        }

        /// <summary>
        /// "Steal" a resource which was already booked for this reservation.
        /// Caution: An exception will be thrown if the given resource was
        /// not contained in this reservation.
        /// </summary>
        /// <param name="resource">A resource to take away from this reservation.</param>
        /// <returns>The stolen resource.</returns>
        public IResource Steal(IResource resource)
        {
            Type t = resource.GetType();
            if (!AvailableItems.ContainsKey(t) || !AvailableItems[t].Contains(resource))
                throw new ArgumentException("The resource \"" + resource.EntityName + "\" wasn't found in this reservation.", "resource");
            AvailableItems[t].Remove(resource);
            MissingItems[t] += 1;
            finishedCounter += 1;
            return resource;
        }

        #endregion
    }
}