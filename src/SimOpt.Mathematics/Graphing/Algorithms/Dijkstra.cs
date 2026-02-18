using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.Network;
using SimOpt.Mathematics.Graphing.Interfaces;

namespace SimOpt.Mathematics.Graphing.Algorithms
{
    #region default implementations

    /// <summary>
    /// Dijkstra algorithm for <code>double</code> distances
    /// 
    /// Non negative weights greedy algorithm after Edsger W. Dijkstra
    /// extractMin : O(log n)
    /// complete: O(n log n + m) (what the heck is m?)
    /// 
    /// Dijkstra, E. W. (1959). "A note on two problems in connexion with graphs". Numerische Mathematik 1: 269–271.
    /// </summary>
    [Serializable]
    public class Dijkstra : Dijkstra<double>
    {
        public Dijkstra(double sourceDistance = 0, double maxDistance = double.PositiveInfinity, double zeroDistance = 0) 
            : base((d1, d2) => d1 + d2, maxDistance, zeroDistance, sourceDistance)
        {

        }
    }

    /// <summary>
    /// Dijkstra algorithm for <code>int</code> distances
    /// 
    /// Non negative weights greedy algorithm after Edsger W. Dijkstra
    /// extractMin : O(log n)
    /// complete: O(n log n + m) (what the heck is m?)
    /// 
    /// Dijkstra, E. W. (1959). "A note on two problems in connexion with graphs". Numerische Mathematik 1: 269–271.
    /// </summary>
    [Serializable]
    public class DijkstraInt : Dijkstra<int>
    {
        public DijkstraInt(int sourceDistance = 0, int maxDistance = int.MaxValue, int zeroDistance = 0)
            : base((i1, i2) => i1 + i2, maxDistance, zeroDistance, sourceDistance)
        {

        }
    }

    /// <summary>
    /// Dijkstra algorithm for <code>long</code> distances
    /// 
    /// Non negative weights greedy algorithm after Edsger W. Dijkstra
    /// extractMin : O(log n)
    /// complete: O(n log n + m) (what the heck is m?)
    /// 
    /// Dijkstra, E. W. (1959). "A note on two problems in connexion with graphs". Numerische Mathematik 1: 269–271.
    /// </summary>
    [Serializable]
    public class DijkstraLong : Dijkstra<long>
    {
        public DijkstraLong(long sourceDistance = 0, long maxDistance = long.MaxValue, long zeroDistance = 0)
            : base((l1, l2) => l1 + l2, maxDistance, zeroDistance, sourceDistance)
        {

        }
    }

    /// <summary>
    /// Dijkstra algorithm for <code>decimal</code> distances
    /// 
    /// Non negative weights greedy algorithm after Edsger W. Dijkstra
    /// extractMin : O(log n)
    /// complete: O(n log n + m) (what the heck is m?)
    /// 
    /// Dijkstra, E. W. (1959). "A note on two problems in connexion with graphs". Numerische Mathematik 1: 269–271.
    /// </summary>
    [Serializable]
    public class DijkstraDecimal : Dijkstra<decimal>
    {
        public DijkstraDecimal(decimal sourceDistance = 0, decimal maxDistance = decimal.MaxValue, decimal zeroDistance = 0)
            : base((d1, d2) => d1 + d2, maxDistance, zeroDistance, sourceDistance)
        {

        }
    }

    /// <summary>
    /// Dijkstra algorithm for <code>float</code> distances
    /// 
    /// Non negative weights greedy algorithm after Edsger W. Dijkstra
    /// extractMin : O(log n)
    /// complete: O(n log n + m) (what the heck is m?)
    /// 
    /// Dijkstra, E. W. (1959). "A note on two problems in connexion with graphs". Numerische Mathematik 1: 269–271.
    /// </summary>
    [Serializable]
    public class DijkstraFloat : Dijkstra<float>
    {
        public DijkstraFloat(float sourceDistance = 0, float maxDistance = float.MaxValue, float zeroDistance = 0)
            : base((f1, f2) => f1 + f2, maxDistance, zeroDistance, sourceDistance)
        {

        }
    }

    #endregion

    /// <summary>
    /// Generic Dijkstra algorithm
    /// 
    /// Non negative weights greedy algorithm after Edsger W. Dijkstra
    /// extractMin : O(log n)
    /// complete: O(n log n + m) (what the heck is m?)
    /// 
    /// Dijkstra, E. W. (1959). "A note on two problems in connexion with graphs". Numerische Mathematik 1: 269–271.
    /// </summary>
    [Serializable]
    public class Dijkstra<T> : IGraphAnalyzer<T>, IPathFinder<T>
        where T : IComparable<T>
    {
        #region cvar

        // distance values
        private T zeroDistance;     // usually zero
        private T sourceDistance;   // usually zero
        private T maxDistance;      // usually infinite

        // temporary list
        private List<INode<T>> notVisitedNodes;

        // no operator interfaces in c# :-(
        private Func<T, T, T> addDistances;

        // caching
        private Dictionary<INode<T>, Dictionary<INode<T>, INode<T>>> predecessorBySource;
        private Dictionary<INode<T>, Dictionary<INode<T>, T>> distancesBySource;
        private Dictionary<INode<T>, bool> isInitialized;

        #endregion
        #region ctor

        /// <summary>
        /// create an instance of the dijkstra algorithm
        /// </summary>
        /// <param name="addDistances"></param>
        /// <param name="maxDistance"></param>
        /// <param name="zeroDistance"></param>
        /// <param name="sourceDistance"></param>
        public Dijkstra(Func<T, T, T> addDistances, T maxDistance, T zeroDistance = default(T), T sourceDistance = default(T))
        {
            predecessorBySource = new Dictionary<INode<T>, Dictionary<INode<T>, INode<T>>>();
            distancesBySource = new Dictionary<INode<T>, Dictionary<INode<T>, T>>();
            isInitialized = new Dictionary<INode<T>, bool>();
            notVisitedNodes = new List<INode<T>>();

            this.zeroDistance = zeroDistance;
            this.sourceDistance = sourceDistance;
            this.maxDistance = maxDistance;
            this.addDistances = addDistances;
        }

        #endregion
        #region impl

        #region public

        /// <summary>
        /// O(log n) (?)
        /// CAUTION: if this has already be done, the values 
        /// will be re-calculated.
        /// </summary>
        /// <param name="network"></param>
        public void AnalyzeGraphForSource(INetwork<T> network, INode<T> source)
        {
            Initialize(network, source);
            FindAll(source);
        }

        /// <summary>
        /// CAUTION! O(n log n) (?), huge memory consumption!
        /// CAUTION: if this has already be done, the values 
        /// will be re-calculated.
        /// </summary>
        /// <param name="network"></param>
        public void AnalyzeGraphComplete(INetwork<T> network)
        {
            foreach (INode<T> node in network.Nodes)
            {
                Initialize(network, node);
                FindAll(node);
            }
        }

        /// <summary>
        /// CAUTION! O(n log n) (?), huge memory consumption!
        /// CAUTION: if this has already be done, the values 
        /// will be re-calculated.
        /// </summary>
        /// <param name="network"></param>
        public void AnalyzeGraph(INetwork<T> network)
        {
            AnalyzeGraphComplete(network);
        }

        /// <summary>
        /// calculates the shortest path between the given nodes.
        /// CAUTION: caching just stores all calculated data permanently.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="fromNode"></param>
        /// <param name="toNode"></param>
        /// <param name="result"></param>
        /// <param name="cacheDistances"></param>
        /// <returns></returns>
        public bool FindShortestPath(INetwork<T> network, INode<T> fromNode, INode<T> toNode, out IPath<T> result, bool cacheData = true)
        {
            // path already calculated?
            if (HasPredecessorFromSource(toNode, fromNode))
            {
                result = ConstructPath(network, fromNode, toNode);
                return true;
            }

            Initialize(network, fromNode);
            bool success = Find(fromNode, toNode);

            if (!cacheData)
            {
                foreach (INode<T> node in network.Nodes)
                    ClearCache(node, fromNode);
            }

            result = ConstructPath(network, fromNode, toNode);

            return success;
        }

        /// <summary>
        /// clear all cached network data
        /// </summary>
        /// <param name="network"></param>
        public void ClearCache(INetwork<T> network)
        {
            foreach (INode<T> node in network.Nodes)
            {
                ClearCache(node);
            }
        }

        #endregion
        #region algorithm

        private void Initialize(INetwork<T> network, INode<T> source)
        {
            notVisitedNodes.Clear();

            foreach (INode<T> node in network.Nodes)
            {
                Initialize(node);
                if (node.Equals(source))
                    distancesBySource[node][source] = sourceDistance;
                else
                    distancesBySource[node][source] = maxDistance;
                notVisitedNodes.Add(node);
            }
        }

        private void FindAll(INode<T> fromSource)
        {
            while (notVisitedNodes.Count > 0)
            {
                // sort the nodes by distance from the source
                notVisitedNodes.Sort((first, second) => distancesBySource[first][fromSource].CompareTo(distancesBySource[second][fromSource]));

                // get the one with the smallest distance
                INode<T> current = notVisitedNodes.First();

                // stop if no further nodes are reachable
                if (distancesBySource[current][fromSource].Equals(maxDistance))
                    return; // all further nodes are unreachable

                // update todo list
                notVisitedNodes.Remove(current);

                // visit all neighbours
                foreach (INode<T> node in current.ConnectedNodes)
                {
                    // calculate distance from source over the current node
                    T dist = addDistances.Invoke(distancesBySource[current][fromSource], node.DistanceTo(current));

                    // if this is the shortest path, set or update distance and predecessor
                    if (dist.CompareTo(distancesBySource[node][fromSource]) < 0)
                    {
                        distancesBySource[node][fromSource] = dist;
                        predecessorBySource[node][fromSource] = current;
                    }
                }
            }
        }

        private bool Find(INode<T> fromSource, INode<T> toTarget)
        {
            while (notVisitedNodes.Count > 0)
            {
                // sort the nodes by distance from the source
                notVisitedNodes.Sort((first, second) => distancesBySource[first][fromSource].CompareTo(distancesBySource[second][fromSource]));

                // get the one with the smallest distance
                INode<T> current = notVisitedNodes.First();

                // stop if no further nodes are reachable
                if (distancesBySource[current][fromSource].Equals(maxDistance))
                    return false; // all further nodes are unreachable

                // update todo list
                notVisitedNodes.Remove(current);

                // target reached?
                if (current.Equals(toTarget)) return true;

                // visit all neighbours
                foreach (INode<T> node in current.ConnectedNodes)
                {
                    // calculate distance from source over the current node
                    T dist = addDistances.Invoke(distancesBySource[current][fromSource], node.DistanceTo(current));

                    // if this is the shortest path, set or update distance and predecessor
                    if (dist.CompareTo(distancesBySource[node][fromSource]) < 0)
                    {
                        distancesBySource[node][fromSource] = dist;
                        predecessorBySource[node][fromSource] = current;
                    }
                }
            }
            return false;
        }

        #endregion

        #endregion
        #region util

        private IPath<T> ConstructPath(INetwork<T> network, INode<T> fromNode, INode<T> toNode)
        {
            IPath<T> result = network.CreateEmptyPath();
            INode<T> last = toNode;
            result.AppendNode(last);
            while (predecessorBySource[last].ContainsKey(fromNode))
            {
                last = predecessorBySource[last][fromNode];
                result.PrependNode(last);
            }
            return result;
        }

        private bool IsInitialized(INode<T> node)
        {
            return isInitialized.ContainsKey(node) && isInitialized[node];
        }

        private void Initialize(INode<T> node)
        {
            if (IsInitialized(node)) return;
            predecessorBySource[node] = new Dictionary<INode<T>, INode<T>>();
            distancesBySource[node] = new Dictionary<INode<T>, T>();
            isInitialized[node] = true;
        }

        private void ClearCache(INode<T> node)
        {
            predecessorBySource.Remove(node);
            distancesBySource.Remove(node);
            isInitialized[node] = false;
        }

        private void ClearCache(INode<T> node, INode<T> forSource)
        {
            predecessorBySource[node].Remove(forSource);
            distancesBySource[node].Remove(forSource);
        }

        private bool HasDistanceFromSource(INode<T> node, INode<T> source)
        {
            if (!IsInitialized(node)) return false;
            return distancesBySource[node].ContainsKey(source);
        }

        private bool HasPredecessorFromSource(INode<T> node, INode<T> source)
        {
            if (!IsInitialized(node)) return false;
            return predecessorBySource[node].ContainsKey(source);
        }

        #endregion
    }
}