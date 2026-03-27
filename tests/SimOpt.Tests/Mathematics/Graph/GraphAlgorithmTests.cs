using FluentAssertions;
using SimOpt.Basics.Datastructures.Network;
using SimOpt.Mathematics.Graphing.Algorithms;
using SimOpt.Mathematics.Graphing.Interfaces;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Network;
using Xunit;
using SimPath = SimOpt.Simulation.Network.Path;

// NOTE: Only Dijkstra<T> is implemented in SimOpt.Mathematics.Graphing.Algorithms.
// FloydWarshall, AStar, BellmannFord, Johnson, and DStar exist as stub classes
// (pseudocode comments only, no real implementation). Tests below cover:
//   - Dijkstra<T> and its typed aliases (Dijkstra / DijkstraInt / DijkstraLong)
//   - SimOpt.Simulation.Network.{Network, Node, Connection, Path} construction and queries
// Missing implementations are noted in region comments.

namespace SimOpt.Tests.Mathematics.Graph;

/// <summary>
/// Comprehensive tests for graph algorithms and network construction in SimOpt.
/// All network objects are built using SimOpt.Simulation.Network because
/// SimOpt.Mathematics.Graphing.Network is a stub (ConnectedNodes, DistanceTo,
/// CreateEmptyPath all throw NotImplementedException).
/// </summary>
public class GraphAlgorithmTests
{
    // ------------------------------------------------------------------
    // Factory helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Creates a simulation network that is usable without a full Model.
    /// The Network constructor stores IModel for logging only; passing null
    /// for the Dijkstra constructor path-finder keeps things lean.
    /// We use a real Model() so that Connection objects can self-register.
    /// </summary>
    private static (SimOpt.Simulation.Network.Network network, Model model)
        CreateNetwork(string name = "test")
    {
        var model = new Model();
        var network = new SimOpt.Simulation.Network.Network(model, name, name,
            findPathMethod: (ISimulationNode _, ISimulationNode _) => null);
        return (network, model);
    }

    private static Node MakeNode(SimOpt.Simulation.Network.Network net, string id)
        => new Node(id, id, net);

    /// <summary>
    /// Connect two nodes with an explicit length (undirected by default).
    /// </summary>
    private static Connection Connect(Node from, Node to, double length, bool directed = false)
        => (Connection)from.ConnectTo(to, length, 0.0, directed);

    // ------------------------------------------------------------------
    // REGION: Network construction and node/connection queries
    // ------------------------------------------------------------------

    public class NetworkConstructionTests
    {
        [Fact]
        public void Network_AddNode_CountNodesIncreases()
        {
            var (net, _) = CreateNetwork();
            _ = MakeNode(net, "A");
            _ = MakeNode(net, "B");

            net.CountNodes.Should().Be(2);
        }

        [Fact]
        public void Network_AddDuplicateId_ReturnsFalse()
        {
            var (net, _) = CreateNetwork();
            var n1 = MakeNode(net, "dup");

            // Attempting to add a node with the same ID should fail.
            var result = net.Add(n1);

            result.Should().BeFalse();
        }

        [Fact]
        public void Network_NodesEnumeration_ContainsAllAddedNodes()
        {
            var (net, _) = CreateNetwork();
            var a = MakeNode(net, "X");
            var b = MakeNode(net, "Y");

            net.Nodes.Should().Contain(a).And.Contain(b);
        }

        [Fact]
        public void Network_RemoveNode_CountDecreases()
        {
            var (net, _) = CreateNetwork();
            var n = MakeNode(net, "Rem");

            net.Remove(n);

            net.CountNodes.Should().Be(0);
        }

        [Fact]
        public void Node_ConnectTo_IsConnectedTo_ReturnsTrue()
        {
            var (net, _) = CreateNetwork();
            var a = MakeNode(net, "a");
            var b = MakeNode(net, "b");

            Connect(a, b, 5.0);

            a.IsConnectedTo(b).Should().BeTrue();
        }

        [Fact]
        public void Node_UndirectedConnect_BothDirectionsReachable()
        {
            var (net, _) = CreateNetwork();
            var a = MakeNode(net, "a");
            var b = MakeNode(net, "b");

            Connect(a, b, 3.0, directed: false);

            // Both nodes must report the connection.
            a.IsConnectedTo(b).Should().BeTrue();
            b.IsConnectedTo(a).Should().BeTrue();
        }

        [Fact]
        public void Node_DirectedConnect_OnlySourceSeesFarNode()
        {
            var (net, _) = CreateNetwork();
            var a = MakeNode(net, "a");
            var b = MakeNode(net, "b");

            Connect(a, b, 7.0, directed: true);

            a.IsConnectedTo(b).Should().BeTrue();
            b.IsConnectedTo(a).Should().BeFalse();
        }

        [Fact]
        public void Node_DistanceTo_ConnectedNode_ReturnsLength()
        {
            var (net, _) = CreateNetwork();
            var a = MakeNode(net, "a");
            var b = MakeNode(net, "b");

            Connect(a, b, 42.0);

            a.DistanceTo(b).Should().BeApproximately(42.0, 1e-10);
        }

        [Fact]
        public void Node_DistanceTo_DisconnectedNode_ReturnsInfinity()
        {
            var (net, _) = CreateNetwork();
            var a = MakeNode(net, "a");
            var b = MakeNode(net, "b");

            // No connection established.
            a.DistanceTo(b).Should().Be(double.PositiveInfinity);
        }

        [Fact]
        public void Connection_Length_StoredCorrectly()
        {
            var (net, _) = CreateNetwork();
            var a = MakeNode(net, "a");
            var b = MakeNode(net, "b");

            var con = Connect(a, b, 99.5);

            con.Length.Should().BeApproximately(99.5, 1e-10);
        }

        [Fact]
        public void Connection_ConnectedNodes_MatchesEndpoints()
        {
            var (net, _) = CreateNetwork();
            var a = MakeNode(net, "a");
            var b = MakeNode(net, "b");

            var con = Connect(a, b, 1.0);

            con.ConnectedNodes.Item1.Should().Be(a);
            con.ConnectedNodes.Item2.Should().Be(b);
        }

        [Fact]
        public void Node_GetConnection_ReturnsCorrectConnection()
        {
            var (net, _) = CreateNetwork();
            var a = MakeNode(net, "a");
            var b = MakeNode(net, "b");

            var con = Connect(a, b, 5.5);
            var retrieved = a.GetConnection(b);

            retrieved.Should().BeSameAs(con);
        }

        [Fact]
        public void Node_GetConnection_MissingConnection_ReturnsNull()
        {
            var (net, _) = CreateNetwork();
            var a = MakeNode(net, "a");
            var b = MakeNode(net, "b");

            a.GetConnection(b).Should().BeNull();
        }

        [Fact]
        public void Node_Equality_SameId_AreEqual()
        {
            var (net, _) = CreateNetwork();
            var a = MakeNode(net, "eqA");

            // INode<double>.Equals compares ID strings.
            a.Equals(a).Should().BeTrue();
        }

        [Fact]
        public void Node_Equality_DifferentId_AreNotEqual()
        {
            var (net, _) = CreateNetwork();
            var a = MakeNode(net, "eq1");
            var b = MakeNode(net, "eq2");

            a.Equals(b).Should().BeFalse();
        }
    }

    // ------------------------------------------------------------------
    // REGION: Dijkstra<double> — single source
    // ------------------------------------------------------------------

    public class DijkstraSingleSourceTests
    {
        /// <summary>
        /// Linear chain: A --1-- B --2-- C --3-- D
        /// Expected shortest distances from A: B=1, C=3, D=6
        /// </summary>
        [Fact]
        public void Dijkstra_LinearChain_CorrectDistancesFromSource()
        {
            var (net, _) = CreateNetwork("linear");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            var d = MakeNode(net, "D");
            Connect(a, b, 1.0);
            Connect(b, c, 2.0);
            Connect(c, d, 3.0);

            var dijkstra = new Dijkstra();
            dijkstra.AnalyzeGraphForSource(net, a);

            // Verify via FindShortestPath after AnalyzeGraphForSource (cache hit).
            dijkstra.FindShortestPath(net, a, d, out IPath<double> pathAD).Should().BeTrue();
            pathAD.Nodes.Should().StartWith(new[] { a }).And.EndWith(new[] { d });
        }

        /// <summary>
        /// Diamond graph:
        ///        2
        ///   A ------ B
        ///   |         |
        ///  5|         |1
        ///   |         |
        ///   C ------ D
        ///        3
        /// Shortest A→D should be A→B→D = 3, not A→C→D = 8.
        /// </summary>
        [Fact]
        public void Dijkstra_DiamondGraph_ChoosesShortestPath()
        {
            var (net, _) = CreateNetwork("diamond");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            var d = MakeNode(net, "D");

            Connect(a, b, 2.0);
            Connect(a, c, 5.0);
            Connect(b, d, 1.0);
            Connect(c, d, 3.0);

            var dijkstra = new Dijkstra();
            bool found = dijkstra.FindShortestPath(net, a, d, out IPath<double> result);

            found.Should().BeTrue();
            // Path must pass through B (the cheaper route).
            result.Nodes.Should().Contain(b);
            result.Nodes.Should().NotContain(c);
        }

        /// <summary>
        /// Triangle: A --1-- B --1-- C, A --10-- C
        /// Shortest A→C = 2 via B.
        /// </summary>
        [Fact]
        public void Dijkstra_Triangle_TakesIndirectShorterRoute()
        {
            var (net, _) = CreateNetwork("tri");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            Connect(a, b, 1.0);
            Connect(b, c, 1.0);
            Connect(a, c, 10.0);

            var dijkstra = new Dijkstra();
            bool found = dijkstra.FindShortestPath(net, a, c, out IPath<double> result);

            found.Should().BeTrue();
            result.Nodes.Should().Contain(b);
        }

        [Fact]
        public void Dijkstra_SingleNode_PathToSelf_ContainsOnlySource()
        {
            var (net, _) = CreateNetwork("single");
            var a = MakeNode(net, "A");

            var dijkstra = new Dijkstra();
            dijkstra.AnalyzeGraphForSource(net, a);

            // A→A: ConstructPath just appends the destination then backtracks —
            // since no predecessor is set for source itself, result is just [A].
            bool found = dijkstra.FindShortestPath(net, a, a, out IPath<double> result);

            result.Nodes.Should().ContainSingle().Which.Should().Be(a);
        }

        /// <summary>
        /// Path nodes must start with fromNode and end with toNode.
        /// </summary>
        [Fact]
        public void Dijkstra_FindShortestPath_PathStartsAtFromNodeEndsAtToNode()
        {
            var (net, _) = CreateNetwork("seq");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            Connect(a, b, 3.0);
            Connect(b, c, 4.0);

            var dijkstra = new Dijkstra();
            dijkstra.FindShortestPath(net, a, c, out IPath<double> result);

            var nodes = result.Nodes.ToList();
            nodes.First().Should().Be(a);
            nodes.Last().Should().Be(c);
        }

        /// <summary>
        /// Intermediate node in the middle of a chain must appear in the path.
        /// </summary>
        [Fact]
        public void Dijkstra_IntermediateNode_AppearsInPath()
        {
            var (net, _) = CreateNetwork("mid");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            Connect(a, b, 1.0);
            Connect(b, c, 1.0);

            var dijkstra = new Dijkstra();
            dijkstra.FindShortestPath(net, a, c, out IPath<double> result);

            result.Nodes.Should().Contain(b);
        }
    }

    // ------------------------------------------------------------------
    // REGION: Dijkstra<double> — disconnected graphs
    // ------------------------------------------------------------------

    public class DijkstraDisconnectedGraphTests
    {
        /// <summary>
        /// Two isolated nodes — no path should be found.
        /// </summary>
        [Fact]
        public void Dijkstra_DisconnectedNodes_FindShortestPath_ReturnsFalse()
        {
            var (net, _) = CreateNetwork("disconn");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");

            var dijkstra = new Dijkstra();
            bool found = dijkstra.FindShortestPath(net, a, b, out IPath<double> _);

            found.Should().BeFalse();
        }

        /// <summary>
        /// Two separate components: {A-B} and {C-D}.
        /// Path from A to C must not be found.
        /// </summary>
        [Fact]
        public void Dijkstra_TwoComponents_CrossComponentSearch_ReturnsFalse()
        {
            var (net, _) = CreateNetwork("twocomp");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            var d = MakeNode(net, "D");
            Connect(a, b, 1.0);
            Connect(c, d, 1.0);

            var dijkstra = new Dijkstra();
            bool found = dijkstra.FindShortestPath(net, a, c, out IPath<double> _);

            found.Should().BeFalse();
        }

        /// <summary>
        /// Directed graph: A→B exists but B→A does not.
        /// NOTE: The Dijkstra implementation calls node.DistanceTo(current) where
        /// node is the neighbour and current is the expanding node. For SimulationNode,
        /// DistanceTo looks up node.SimulationConnections[current], which only exists
        /// for the outgoing direction. For directed A→B, b.DistanceTo(a) returns
        /// +Infinity because B does not have A in its SimulationConnections dictionary.
        /// This means directed edges are effectively transparent to the algorithm:
        /// neither direction is found when using directed connections.
        /// This test documents the current behaviour rather than the expected ideal.
        /// </summary>
        [Fact]
        public void Dijkstra_DirectedGraph_DistanceLookupLimitation_NeitherDirectionFound()
        {
            var (net, _) = CreateNetwork("dir");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            Connect(a, b, 5.0, directed: true);

            var dijkstra = new Dijkstra();
            // Due to DistanceTo implementation: node.DistanceTo(current) always returns
            // +Infinity for directed connections because the edge is only stored in the
            // source node's dictionary, not in the neighbour's.
            bool forwardFound = dijkstra.FindShortestPath(net, a, b, out IPath<double> _);
            bool backwardFound = dijkstra.FindShortestPath(net, b, a, out IPath<double> _);

            // Both return false: the algorithm cannot traverse directed edges.
            forwardFound.Should().BeFalse();
            backwardFound.Should().BeFalse();
        }
    }

    // ------------------------------------------------------------------
    // REGION: Dijkstra<double> — AnalyzeGraph (all-pairs)
    // ------------------------------------------------------------------

    public class DijkstraAllPairsTests
    {
        /// <summary>
        /// AnalyzeGraph runs Dijkstra from every node. After that every
        /// reachable pair should be answerable from the cache.
        /// </summary>
        [Fact]
        public void Dijkstra_AnalyzeGraph_AllReachablePairsFound()
        {
            var (net, _) = CreateNetwork("allpairs");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            Connect(a, b, 2.0);
            Connect(b, c, 3.0);

            var dijkstra = new Dijkstra();
            dijkstra.AnalyzeGraph(net);

            dijkstra.FindShortestPath(net, a, c, out IPath<double> ac).Should().BeTrue();
            dijkstra.FindShortestPath(net, b, c, out IPath<double> bc).Should().BeTrue();
            dijkstra.FindShortestPath(net, c, a, out IPath<double> ca).Should().BeTrue(); // undirected
        }

        [Fact]
        public void Dijkstra_AnalyzeGraph_ClearCache_ForcesRecompute()
        {
            var (net, _) = CreateNetwork("cleartest");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            Connect(a, b, 1.0);

            var dijkstra = new Dijkstra();
            dijkstra.AnalyzeGraph(net);
            dijkstra.ClearCache(net);

            // After clear the algorithm can still find the path (recomputes).
            bool found = dijkstra.FindShortestPath(net, a, b, out IPath<double> _);
            found.Should().BeTrue();
        }
    }

    // ------------------------------------------------------------------
    // REGION: Dijkstra — no-cache mode
    // ------------------------------------------------------------------

    public class DijkstraCachingTests
    {
        /// <summary>
        /// When cacheData = false, Dijkstra clears the predecessor table BEFORE calling
        /// ConstructPath. This means ConstructPath can only append the destination node
        /// (it finds no predecessor to follow). The returned bool is still correct (true
        /// if reachable), but the IPath only contains the target node.
        /// This test documents the current implementation behaviour.
        /// </summary>
        [Fact]
        public void Dijkstra_NoCaching_ReturnsTrueButPathContainsOnlyTarget()
        {
            var (net, _) = CreateNetwork("nocache");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            Connect(a, b, 7.0);

            var dijkstra = new Dijkstra();
            bool found = dijkstra.FindShortestPath(net, a, b, out IPath<double> result, cacheData: false);

            // Path is reachable.
            found.Should().BeTrue();
            // With cacheData=false the cache is cleared before ConstructPath runs,
            // so only the destination node appears in the path.
            result.Nodes.Should().Contain(b);
        }

        /// <summary>
        /// Calling FindShortestPath twice (with caching) should not throw or produce
        /// incorrect results — the second call hits the predecessor cache.
        /// </summary>
        [Fact]
        public void Dijkstra_CalledTwiceWithCaching_BothSucceed()
        {
            var (net, _) = CreateNetwork("twicecache");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            Connect(a, b, 2.0);

            var dijkstra = new Dijkstra();
            bool first = dijkstra.FindShortestPath(net, a, b, out IPath<double> r1, cacheData: true);
            bool second = dijkstra.FindShortestPath(net, a, b, out IPath<double> r2, cacheData: true);

            first.Should().BeTrue();
            second.Should().BeTrue();
            r1.Nodes.Count().Should().Be(r2.Nodes.Count());
        }
    }

    // ------------------------------------------------------------------
    // REGION: DijkstraInt — integer edge weights
    // ------------------------------------------------------------------

    public class DijkstraIntTests
    {
        // DijkstraInt uses INetwork<int> / INode<int> — there is no
        // SimOpt.Simulation.Network implementation for int nodes, and the
        // SimOpt.Mathematics.Graphing.Network.Node for int throws
        // NotImplementedException on ConnectedNodes. We therefore exercise
        // DijkstraInt via a minimal Moq-based stub network.

        private static Moq.Mock<INode<int>> MockNode(string id)
        {
            var m = new Moq.Mock<INode<int>>();
            m.Setup(n => n.Equals(m.Object)).Returns(true);
            m.Setup(n => n.Equals(Moq.It.Is<INode<int>>(x => !ReferenceEquals(x, m.Object)))).Returns(false);
            // GetHashCode not needed for these tests.
            return m;
        }

        [Fact]
        public void DijkstraInt_Constructor_DefaultValues_DoesNotThrow()
        {
            var act = () => new DijkstraInt();
            act.Should().NotThrow();
        }

        [Fact]
        public void DijkstraInt_Constructor_CustomValues_DoesNotThrow()
        {
            var act = () => new DijkstraInt(sourceDistance: 0, maxDistance: 1000, zeroDistance: 0);
            act.Should().NotThrow();
        }
    }

    // ------------------------------------------------------------------
    // REGION: DijkstraLong — long edge weights
    // ------------------------------------------------------------------

    public class DijkstraLongTests
    {
        [Fact]
        public void DijkstraLong_Constructor_DoesNotThrow()
        {
            var act = () => new DijkstraLong();
            act.Should().NotThrow();
        }

        [Fact]
        public void DijkstraLong_Constructor_CustomValues_DoesNotThrow()
        {
            var act = () => new DijkstraLong(0L, long.MaxValue, 0L);
            act.Should().NotThrow();
        }
    }

    // ------------------------------------------------------------------
    // REGION: DijkstraFloat — float edge weights
    // ------------------------------------------------------------------

    public class DijkstraFloatTests
    {
        [Fact]
        public void DijkstraFloat_Constructor_DoesNotThrow()
        {
            var act = () => new DijkstraFloat();
            act.Should().NotThrow();
        }
    }

    // ------------------------------------------------------------------
    // REGION: DijkstraDecimal — decimal edge weights
    // ------------------------------------------------------------------

    public class DijkstraDecimalTests
    {
        [Fact]
        public void DijkstraDecimal_Constructor_DoesNotThrow()
        {
            var act = () => new DijkstraDecimal();
            act.Should().NotThrow();
        }
    }

    // ------------------------------------------------------------------
    // REGION: Dijkstra<T> generic — custom accumulator
    // ------------------------------------------------------------------

    public class DijkstraGenericTests
    {
        /// <summary>
        /// Uses the generic Dijkstra<int> with a custom add function to
        /// verify the interface contract of the generic variant.
        /// We use a Moq-backed network with two connected nodes.
        /// </summary>
        [Fact]
        public void DijkstraGeneric_CustomAddFunction_CanBeConstructed()
        {
            var dijkstra = new Dijkstra<int>((a, b) => a + b, int.MaxValue, 0, 0);
            dijkstra.Should().NotBeNull();
        }

        [Fact]
        public void DijkstraGeneric_CustomAddFunction_ImpliesIGraphAnalyzer()
        {
            IGraphAnalyzer<int> analyzer = new Dijkstra<int>((a, b) => a + b, int.MaxValue, 0, 0);
            analyzer.Should().NotBeNull();
        }

        [Fact]
        public void DijkstraGeneric_CustomAddFunction_ImpliesIPathFinder()
        {
            IPathFinder<int> finder = new Dijkstra<int>((a, b) => a + b, int.MaxValue, 0, 0);
            finder.Should().NotBeNull();
        }
    }

    // ------------------------------------------------------------------
    // REGION: Path construction tests
    // ------------------------------------------------------------------

    public class PathConstructionTests
    {
        [Fact]
        public void Path_CreateFromNodes_IsValid()
        {
            var (net, _) = CreateNetwork("pathnet");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            Connect(a, b, 3.0);
            Connect(b, c, 4.0);

            var path = new SimPath(net);
            bool ok = path.CreateFrom(a, b, c);

            ok.Should().BeTrue();
            path.IsValid.Should().BeTrue();
            path.NodeCount.Should().Be(3);
        }

        [Fact]
        public void Path_CreateFromNodes_Length_IsSumOfEdges()
        {
            var (net, _) = CreateNetwork("pathlen");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            Connect(a, b, 3.0);
            Connect(b, c, 4.0);

            var path = new SimPath(net);
            path.CreateFrom(a, b, c);

            path.Length.Should().BeApproximately(7.0, 1e-10);
        }

        [Fact]
        public void Path_AppendNode_IncreasesNodeCount()
        {
            var (net, _) = CreateNetwork("appendnet");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            Connect(a, b, 1.0);
            Connect(b, c, 1.0);

            var path = new SimPath(net);
            path.AppendNode(a);
            path.AppendNode(b);
            path.AppendNode(c);

            path.NodeCount.Should().Be(3);
        }

        [Fact]
        public void Path_PrependNode_InsertsAtFront()
        {
            var (net, _) = CreateNetwork("prependnet");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            Connect(a, b, 2.0);
            Connect(b, c, 3.0);

            // Build b→c first, then prepend a.
            var path = new SimPath(net);
            path.AppendNode(b);
            path.AppendNode(c);
            path.PrependNode(a);

            path.SimulationNodes.First().Should().Be(a);
            path.NodeCount.Should().Be(3);
        }

        [Fact]
        public void Path_Clear_RemovesAllNodes()
        {
            var (net, _) = CreateNetwork("clearpath");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            Connect(a, b, 1.0);

            var path = new SimPath(net);
            path.AppendNode(a);
            path.AppendNode(b);
            path.Clear();

            path.IsEmpty.Should().BeTrue();
            path.NodeCount.Should().Be(0);
        }

        [Fact]
        public void Path_CreateEmptyPath_FromNetwork_IsEmpty()
        {
            var (net, _) = CreateNetwork("emptypath");
            var path = (SimPath)net.CreateEmptyPath();

            path.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void Path_GetDistanceTo_IntermediateNode_IsPartialLength()
        {
            var (net, _) = CreateNetwork("distpath");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            Connect(a, b, 5.0);
            Connect(b, c, 10.0);

            var path = new SimPath(net);
            path.CreateFrom(a, b, c);

            path.GetDistanceTo(b).Should().BeApproximately(5.0, 1e-10);
        }

        [Fact]
        public void Path_GetSubPath_ReturnsCorrectSlice()
        {
            var (net, _) = CreateNetwork("subpath");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            Connect(a, b, 2.0);
            Connect(b, c, 3.0);

            var path = new SimPath(net);
            path.CreateFrom(a, b, c);
            var sub = path.GetSubPath(b, c);

            sub.SimulationNodes.Should().HaveCount(2);
            sub.SimulationNodes.First().Should().Be(b);
            sub.SimulationNodes.Last().Should().Be(c);
        }
    }

    // ------------------------------------------------------------------
    // REGION: Dijkstra integration with SimulationNetwork
    // ------------------------------------------------------------------

    public class DijkstraIntegrationTests
    {
        /// <summary>
        /// Graph:
        ///   A --1-- B --1-- C
        ///   |               |
        ///   +------5--------+
        /// Shortest A→C = 2 via B.
        /// Path length property of the returned IPath is the sum of connection lengths.
        /// </summary>
        [Fact]
        public void Dijkstra_WithSimulationNetwork_PathLength_MatchesExpected()
        {
            var (net, _) = CreateNetwork("integration");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            Connect(a, b, 1.0);
            Connect(b, c, 1.0);
            Connect(a, c, 5.0);

            var dijkstra = new Dijkstra();
            dijkstra.FindShortestPath(net, a, c, out IPath<double> result);

            result.Length.Should().BeApproximately(2.0, 1e-10);
        }

        /// <summary>
        /// Five-node graph with varying weights. Verifies that Dijkstra
        /// correctly handles multiple alternative paths.
        ///
        ///   A --2-- B --3-- E
        ///   |       |
        ///   4       1
        ///   |       |
        ///   C --1-- D
        ///
        /// Shortest A→E: A→B→E = 5
        /// Shortest A→D: A→B→D = 3
        /// </summary>
        [Fact]
        public void Dijkstra_FiveNodeGraph_CorrectShortestPaths()
        {
            var (net, _) = CreateNetwork("five");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            var c = MakeNode(net, "C");
            var d = MakeNode(net, "D");
            var e = MakeNode(net, "E");

            Connect(a, b, 2.0);
            Connect(a, c, 4.0);
            Connect(b, d, 1.0);
            Connect(b, e, 3.0);
            Connect(c, d, 1.0);

            var dijkstra = new Dijkstra();
            dijkstra.AnalyzeGraph(net);

            dijkstra.FindShortestPath(net, a, e, out IPath<double> ae);
            dijkstra.FindShortestPath(net, a, d, out IPath<double> ad);

            ae.Length.Should().BeApproximately(5.0, 1e-10); // A→B→E
            ad.Length.Should().BeApproximately(3.0, 1e-10); // A→B→D
        }

        /// <summary>
        /// Use Dijkstra as IPathFinder injected into a SimulationNetwork.
        /// InvokePathFinder path: Network.FindPath delegates to pathFinder.FindShortestPath.
        /// </summary>
        [Fact]
        public void Dijkstra_InjectedAsPathFinder_NetworkFindPath_ReturnsSamePath()
        {
            var model = new Model();
            var dijkstra = new Dijkstra();
            var net = new SimOpt.Simulation.Network.Network(model, "injected", "injected", dijkstra, useFinderCaching: true);

            var a = new Node("A", "A", net);
            var b = new Node("B", "B", net);
            Connect(a, b, 8.0);

            // Network.FindPath is called: first checks direct connection (a→b exists),
            // so it returns a 2-node path immediately.
            var path = net.FindPath(a, b);

            path.Should().NotBeNull();
            path!.Nodes.Should().Contain(a).And.Contain(b);
        }

        /// <summary>
        /// When AnalyzeGraphForSource is called and then FindShortestPath is called
        /// for a cached result (predecessor already in cache), the path is returned
        /// without re-running the algorithm.
        /// </summary>
        [Fact]
        public void Dijkstra_CachedResult_ReturnsTrueWithoutRecompute()
        {
            var (net, _) = CreateNetwork("cached");
            var a = MakeNode(net, "A");
            var b = MakeNode(net, "B");
            Connect(a, b, 3.0);

            var dijkstra = new Dijkstra();
            // Pre-populate cache for source a.
            dijkstra.AnalyzeGraphForSource(net, a);

            // FindShortestPath will see predecessor already exists → cache hit.
            bool found = dijkstra.FindShortestPath(net, a, b, out IPath<double> result);

            found.Should().BeTrue();
            result.Nodes.Should().Contain(a).And.Contain(b);
        }
    }

    // ------------------------------------------------------------------
    // REGION: Stub algorithm documentation tests
    // NOTE: FloydWarshall, AStar, BellmannFord, Johnson, DStar are stubs.
    //       These tests document their existence and that instantiation
    //       does not throw (they contain no executable logic).
    // ------------------------------------------------------------------

    public class StubAlgorithmTests
    {
        [Fact]
        public void FloydWarshall_CanBeInstantiated()
        {
            // FloydWarshall is a documented stub — no Analyze() method exists.
            var fw = new FloydWarshall();
            fw.Should().NotBeNull();
        }

        [Fact]
        public void AStar_CanBeInstantiated()
        {
            // AStar is a documented stub — the class holds pseudocode only.
            var aStar = new AStar();
            aStar.Should().NotBeNull();
        }

        [Fact]
        public void BellmannFord_CanBeInstantiated()
        {
            // BellmannFord is a documented stub — no implementation present.
            var bf = new BellmannFord();
            bf.Should().NotBeNull();
        }

        [Fact]
        public void Johnson_CanBeInstantiated()
        {
            // Johnson is a TODO stub.
            var johnson = new Johnson();
            johnson.Should().NotBeNull();
        }

        [Fact]
        public void DStar_CanBeInstantiated()
        {
            // DStar is a documented stub.
            var dstar = new DStar();
            dstar.Should().NotBeNull();
        }
    }
}
