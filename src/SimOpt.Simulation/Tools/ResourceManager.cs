using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Interfaces;
using SimOpt.Basics.Exceptions;

namespace SimOpt.Simulation.Tools
{
    /// <summary>
    /// A resource manager using ResourceReservation.
    /// 
    /// This resource manager is intended for the
    /// immediate reservation of resources for an
    /// unspecified duration.
    /// 
    /// TODO: write a resource manager for the scheduled
    /// reservation of items for a fixed duration.
    /// 
    /// TODO: support pre-empting (taking away resources
    /// which are already in use)
    /// 
    /// Please note that the Initialization and Reset of
    /// this resource manager have to be managed manually!
    /// Initialization has to be done after all initial
    /// resources have been added. Reset should be done
    /// after the model was reset. Exception: the default
    /// model class uses an instance of ResourceManager
    /// as the DefaultResourceManager for all IResource
    /// entities where a different resource manager was
    /// not set manually. This instance will be reset
    /// by the model class on reset of the model.
    /// </summary>
    /// <remarks>beta</remarks>
    [Serializable]
    public class ResourceManager : IResourceManager
    {
        #region cvar

        private bool initialized = false;
        private int orderCounter = 0;
        private bool allowStealing = false;
        // private bool allowPreempting = false; // TODO: implement preempting
        private List<IResource> managedResources;
        private List<IResource> initialResources;
        private SortedDictionary<Priority, IResourceReservation> activeReservations;

        #endregion
        #region prop

        /// <summary>
        /// Indicates if reservations with higher priorities can 
        /// "steal" resources which are already booked for
        /// reservations with lower priority.
        /// </summary>
        public bool StealingAllowed { get { return allowStealing; } }

        /// <summary>
        /// All contained resources
        /// </summary>
        public IEnumerable<IResource> ManagedResources { 
            get { return managedResources; } 
        }

        /// <summary>
        /// All currently free resources
        /// </summary>
        public IEnumerable<IResource> FreeResources { 
            get { return from res in managedResources where res.Free select res; } 
        }

        /// <summary>
        /// All currently available resources wich are 
        /// considered for any existing reservation.
        /// These resources are free, but are waiting to
        /// be given to a booker as soon as his whole
        /// reservation is finished.
        /// </summary>
        public IEnumerable<IResource> BookedResources
        {
            get
            {
                foreach (IResourceReservation reservation in activeReservations.Values)
                {
                    foreach (List<IResource> resources in reservation.AvailableItems.Values)
                    {
                        foreach (IResource res in resources)
                        {
                            yield return res;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The current list of active resource reservations
        /// </summary>
        public IEnumerable<IResourceReservation> Reservations 
        {
            get { return activeReservations.Values; }
        }

        #endregion
        #region ctor

        private ResourceManager()
        {
        }


        /// <summary>
        /// CAUTION: You have to initialize the resource manager as
        /// soon as you have added all initial resources
        /// </summary>
        /// <param name="allowStealing"></param>
        public ResourceManager(bool allowStealing = false)
        {
            this.allowStealing = allowStealing;
            managedResources = new List<IResource>();
            initialResources = new List<IResource>();
            activeReservations = new SortedDictionary<Priority, IResourceReservation>();
        }

        #endregion
        #region init

        public void Initialize() 
        {
            initialResources = new List<IResource>(managedResources);
            initialized = true;
        }

        #endregion
        #region rset

        public void Reset() 
        {
            orderCounter = 0;
            managedResources = initialResources;
            activeReservations.Clear();
        }

        #endregion
        #region impl

        #region seizing

        /// <summary>
        /// Seize a single resource
        /// </summary>
        /// <typeparam name="TReceiver"></typeparam>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="forObject"></param>
        /// <param name="pickupMethod"></param>
        /// <param name="checkMethod"></param>
        /// <param name="priority">Note: for equal priorities the FIFO principle applies</param>
        public void Seize<TResource>(IEntity forObject, 
                                     Func<TResource, bool> pickupMethod, 
                                     Func<TResource, bool> checkMethod = null,
                                     Priority priority = null)
            where TResource : IResource
        {
            Dictionary<Type, int> requiredResources = new Dictionary<Type,int>();

            // create a check method if none was provided
            if (checkMethod == null) checkMethod = new Func<TResource, bool>(res => true);
            
            // put it into a dictionary as defined by the full overload
            Dictionary<Type, Func<IResource, bool>> checkMethods = new Dictionary<Type, Func<IResource, bool>>();
            checkMethods[typeof(TResource)] = (input => checkMethod((TResource)input));
            
            // set number of required instances to 1
            requiredResources[typeof(TResource)] = 1;

            // create a forwarder for the pickup method
            ResourcePickupForwarder<TResource> forwarder = new ResourcePickupForwarder<TResource>(pickupMethod);
            
            // pass on to full overload
            Seize(requiredResources, forObject, forwarder.InvokeSingle, checkMethods, priority);
        }

        /// <summary>
        /// Seize a single resource
        /// </summary>
        /// <typeparam name="TReceiver"></typeparam>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="forObject"></param>
        /// <param name="pickupMethod"></param>
        /// <param name="checkMethod"></param>
        public void Seize<TResource>(IEntity forObject,
                                     Func<TResource, bool> pickupMethod,
                                     Func<TResource, bool> checkMethod = null)
            where TResource : IResource
        {
            Seize<TResource>(forObject, pickupMethod, checkMethod, null);
        }

        /// <summary>
        /// Seize a number of resources of the same type
        /// </summary>
        /// <typeparam name="TReceiver"></typeparam>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="nrOfRequiredItems"></param>
        /// <param name="forObject"></param>
        /// <param name="pickupMethod"></param>
        /// <param name="checkMethod"></param>
        /// <param name="priority">Note: for equal priorities the FIFO principle applies</param>
        public void Seize<TResource>(int nrOfRequiredItems, 
                                     IEntity forObject, 
                                     Func<List<TResource>, bool> pickupMethod, 
                                     Func<TResource, bool> checkMethod = null,
                                     Priority priority = null)
            where TResource : IResource
        {
            Dictionary<Type, int> requiredResources = new Dictionary<Type, int>();

            // create a check method if none was given
            if (checkMethod == null) checkMethod = new Func<TResource, bool>(res => true);
            
            // wrap it into a dictionary as required by the full overload
            Dictionary<Type, Func<IResource, bool>> checkMethods = new Dictionary<Type, Func<IResource, bool>>();
            checkMethods[typeof(TResource)] = (input => checkMethod((TResource)input));

            // save the number of required resources into a dictionary as required by the full overload
            requiredResources[typeof(TResource)] = nrOfRequiredItems;

            // create a forwarder for the pickup method
            ResourcePickupForwarder<TResource> forwarder = new ResourcePickupForwarder<TResource>(pickupMethod);

            // pass on to full overload
            Seize(requiredResources, forObject, forwarder.InvokeMultiple, checkMethods, priority);
        }

        /// <summary>
        /// Seize a group of resources of different types, one instance each.
        /// </summary>
        /// <typeparam name="T">Type of the orderer</typeparam>
        /// <param name="requiredResources"></param>
        /// <param name="forObject"></param>
        /// <param name="pickupMethod"></param>
        /// <param name="checkMethods"></param>
        /// <param name="priority">Note: for equal priorities the FIFO principle applies</param>
        public void Seize(Type[] requiredResources, 
                          IEntity forObject, 
                          ResourcesReadyDelegate pickupMethod, 
                          Dictionary<Type, Func<IResource, bool>> checkMethods = null,
                          Priority priority = null) 
        {
            Dictionary<Type, int> tmp = new Dictionary<Type, int>();
            
            // create checkmethods dictionary if none was provided
            if (checkMethods == null) 
                checkMethods = new Dictionary<Type, Func<IResource, bool>>();
            
            // for each required type
            foreach (Type t in requiredResources)
            {
                // add a check method if non was given
                if (!checkMethods.ContainsKey(t) || checkMethods[t] == null)
                    checkMethods[t] = new Func<IResource, bool>(res => true);
                
                // set the number of required instances in the temporary dictionary to 1
                tmp[t] = 1;
            }

            // pass on to the full overload
            Seize(tmp, forObject, pickupMethod, checkMethods, priority);
        }

        /// <summary>
        /// Seize a number given number of instances of the given types of resources each.
        /// </summary>
        /// <typeparam name="T">Type of the orderer</typeparam>
        /// <param name="requiredResources"></param>
        /// <param name="forObject"></param>
        /// <param name="pickupMethod"></param>
        /// <param name="checkMethods"></param>
        /// <param name="priority">Note: for equal priorities the FIFO principle applies</param>
        public void Seize(Dictionary<Type, int> requiredResources, 
                          IEntity forObject, 
                          ResourcesReadyDelegate pickupMethod, 
                          Dictionary<Type, Func<IResource, bool>> checkMethods = null,
                          Priority priority = null) 
        {
            Priority p = priority;
            if (!initialized) throw new InitializationException("The resource manager was not initialized! You must call initialize after having added all initial resources.");
            if (checkMethods == null)
                checkMethods = new Dictionary<Type, Func<IResource, bool>>();
            foreach (Type t in requiredResources.Keys)
            {
                if (requiredResources[t] <= 0) 
                    throw new ArgumentOutOfRangeException("requiredResources", 
                        "The number of ordered resources of the type \"" + 
                        t.FullName + "\" must be greater then 0.");
                if (!checkMethods.ContainsKey(t) || checkMethods[t] == null)
                    checkMethods[t] = new Func<IResource, bool>(res => true);
            }
            ResourceReservation reservation = new ResourceReservation(forObject, requiredResources, pickupMethod, checkMethods);
            if (priority == null) p = new Priority();
            p.AddedOrder = orderCounter;
            orderCounter += 1;
            activeReservations[p] = reservation;
            Update();
        }

        #endregion
        #region management

        /// <summary>
        /// Tell the resource manager that he is responsible for this resouce.
        /// Will throw an exception if the resource is already listed here.
        /// </summary>
        /// <param name="resource"></param>
        public void Manage(IResource resource)
        {
            if (managedResources.Contains(resource)) 
                throw new ArgumentException("The resource " + resource.EntityName + " is already managed by this resource manager.", "resource");
            managedResources.Add(resource);
        }

        /// <summary>
        /// Tell the resource manager that he is responsible for these resouces.
        /// An exception will be thrown if one of the given resources is already
        /// listed in this manager.
        /// </summary>
        /// <param name="resources"></param>
        public void Manage(IEnumerable<IResource> resources) 
        {
            foreach (IResource resource in resources)
                Manage(resource);
        }

        /// <summary>
        /// Tell the resource manager that he is no longer responsible for this resouce.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="force">
        /// If set to true, the given resource will be removed (stolen) 
        /// from an active reservation if it is currently booked. If
        /// stealing is forbidden and force is true an Exception will be thrown.
        /// </param>
        public void UnManage(IResource resource, bool force = false) 
        {
            if (!allowStealing && force) throw new ArgumentException("Stealing has to be enabled to force unmanageing.", "force");
            managedResources.Remove(resource);
            if (force)
            {
                foreach (IResourceReservation reservation in activeReservations.Values)
                {
                    if (reservation.AllAvailableItems.Contains<IResource>(resource))
                        reservation.Steal(resource);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="force">
        /// If set to true, the given resources will be removed (stolen) 
        /// from an active reservation if they are currently booked. If
        /// stealing is forbidden and force is true an Exception will be thrown.
        /// </param>
        public void UnManage(IEnumerable<IResource> resources, bool force = false)
        {
            foreach (IResource resource in resources)
                UnManage(resource);
        }

        #endregion

        /// <summary>
        /// Update all reservations. This has to be called whenever 
        /// a resource is released or a new reservation is placed.
        /// A manual call is usually unnecessary.
        /// </summary>
        public void Update() 
        {
            // Go through all reservations in order of priority
            foreach (KeyValuePair<Priority, IResourceReservation> kvp in activeReservations)
            {
                IResourceReservation reservation = kvp.Value;
                Priority priority = kvp.Key;
                
                // For the current reservation: go through all free resources
                foreach (IResource resource in FreeResources)
                {
                    // If the current reservation is missing a resource of the type
                    // of the current free resource and it is acceptable...
                    if (reservation.MissingItems.ContainsKey(resource.GetType())
                        && reservation.CheckAcceptanceMethods[resource.GetType()].Invoke(resource))
                    {
                        // ...give the resource to the reservation and unfree it.
                        reservation.Put(resource);
                        resource.Free = false;

                        // If the reservation is finished now...
                        if (reservation.Finished)
                        {
                            // If the resources for this finished reservation are not used after all...
                            if (!reservation.PickupMethod.Invoke(reservation.AvailableItems)) 
                            {
                                // ...set them free again...
                                foreach (IResource res in reservation.AllAvailableItems)
                                {
                                    res.Free = true;
                                    res.CurrentHolder = null;
                                }
                                // ...and start from the beginning.
                                goto Restart;
                            } 
                            else // The resources from this reservations are now in use by the orderer
                            {
                                foreach (IResource res in reservation.AllAvailableItems)
                                    res.CurrentHolder = reservation.Orderer;
                            }
                        }
                    }
                }
                if (allowStealing)
                {
                    if (!Steal(priority, reservation)) goto Restart; // stealing has finished a reservation without using the resources
                }
            }

            // All reservations are up to date.
            RemoveFinishedReservations();
            return;

        // A reservation was not used, so now all its resources
        // have been set free. We have to start again to consider 
        // the reservations with the highest priority first.
        Restart:
            RemoveFinishedReservations();
            Update();
        }

        /// <summary>
        /// Steal usable resources from reservations with priority &lt; curPriority for a given reservation
        /// </summary>
        /// <param name="curPriority"></param>
        /// <returns>
        /// false if stealing has finished a reservation 
        /// but the resources have not been used.
        /// </returns>
        private bool Steal(Priority curPriority, IResourceReservation curReservation) 
        {
            if (!allowStealing) throw new InvalidOperationException("Stealing is currently not allowed for this resource manager.");

            IEnumerable<IResourceReservation> lessImportantReservations = from r in activeReservations
                                                                          where r.Key.CompareTo(curPriority) < 0
                                                                          select r.Value;

            foreach (IResourceReservation lessImportantReservation in lessImportantReservations)
            {
                foreach (IResource resource in lessImportantReservation.AllAvailableItems)
                {
                    // If the current reservation is missing a resource of the type
                    // of the current free resource and it is acceptable...
                    if (curReservation.MissingItems.ContainsKey(resource.GetType())
                        && curReservation.CheckAcceptanceMethods[resource.GetType()].Invoke(resource))
                    {
                        // ...give the resource to the reservation and unfree it.
                        lessImportantReservation.Steal(resource);
                        curReservation.Put(resource);
                        // resource.Free = false; // redundant statement

                        // If the reservation is finished now...
                        if (curReservation.Finished)
                        {
                            // If the resources for this finished reservation are not used after all...
                            if (!curReservation.PickupMethod.Invoke(curReservation.AvailableItems))
                            {
                                // ...set them free again...
                                foreach (IResource res in curReservation.AllAvailableItems)
                                {
                                    res.Free = true;
                                    res.CurrentHolder = null;
                                }
                                // ...and start from the beginning.
                                return false;
                            }
                            else // The resources from this reservations are now in use by the orderer
                            {
                                foreach (IResource res in curReservation.AllAvailableItems)
                                    res.CurrentHolder = curReservation.Orderer;
                            }
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// remove all finished reservations from the activeReservations list
        /// </summary>
        private void RemoveFinishedReservations() 
        {
            List<Priority> toRemove = new List<Priority>();
            foreach (KeyValuePair<Priority, IResourceReservation> kvp in activeReservations)
            {
                if (kvp.Value.Finished) toRemove.Add(kvp.Key);
            }
            foreach (Priority p in toRemove)
                activeReservations.Remove(p);
        }

        #endregion

        #region ISerializableGrubi

        // TODO:
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
        //    private bool initialized = false;
        //private int orderCounter = 0;
        //private bool allowStealing = false;
        //// private bool allowPreempting = false; // TODO: implement preempting
        //private List<IResource> managedResources;
        //private List<IResource> initialResources;
        //private SortedDictionary<Priority, IResourceReservation> activeReservations;
        }

        #endregion
    }
}