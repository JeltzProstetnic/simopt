using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Interfaces;

namespace SimOpt.Simulation.Tools
{
    /// <summary>
    /// a helper class for the resource manager to be able
    /// to provide type safety in resource pickup methods for
    /// only one type of resources
    /// </summary>
    /// <typeparam name="T">the type of resource(s) to forward</typeparam>
    public class ResourcePickupForwarder<T> where T : IResource
    {
        private Func<T, bool> singlePickupMethod;
        private Func<List<T>, bool> multiPickupMethod;
        private List<T> resources;

        public ResourcePickupForwarder(Func<T, bool> pickupMethod)
        {
            this.singlePickupMethod = pickupMethod;
        }

        public ResourcePickupForwarder(Func<List<T>, bool> pickupMethod) 
        {
            this.multiPickupMethod = pickupMethod;
            resources = new List<T>();
        }

        public bool InvokeSingle(Dictionary<Type, List<IResource>> resources)
        {
            return singlePickupMethod((T)resources[typeof(T)][0]);
        }

        public bool InvokeMultiple(Dictionary<Type, List<IResource>> resources)
        {
            foreach (IResource res in resources[typeof(T)])
            {
                this.resources.Add((T)res);
            }
            return multiPickupMethod(this.resources);
        }
    }
}
