using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Mathematics.Graphing.Algorithms
{
    /// <summary>
    /// Use Johnson for sparse graphs!
    /// </summary>
    [Serializable]
    public class FloydWarshall
    {
        /*
         * 
         * 
         * The Floyd–Warshall algorithm compares all possible paths through the graph between each pair of vertices. It is able to do this with only V3 comparisons. This is remarkable considering that there may be up to V2 edges in the graph, and every combination of edges is tested. It does so by incrementally improving an estimate on the shortest path between two vertices, until the estimate is known to be optimal.
Consider a graph G with vertices V, each numbered 1 through N. Further consider a function shortestPath(i, j, k) that returns the shortest possible path from i to j using only vertices 1 to k as intermediate points along the way. Now, given this function, our goal is to find the shortest path from each i to each j using only nodes 1 to k + 1.
There are two candidates for this path: either the true shortest path only uses nodes in the set {1, ..., k}; or there exists some path that goes from i to k + 1, then from k + 1 to j that is better. We know that the best path from i to j that only uses nodes 1 through k is defined by shortestPath(i, j, k), and it is clear that if there were a better path from i to k + 1 to j, then the length of this path would be the concatenation of the shortest path from i to k + 1 (using vertices in {1, ..., k}) and the shortest path from k + 1 to j (also using vertices in {1, ..., k}).
Therefore, we can define shortestPath(i, j, k) in terms of the following recursive formula:
shortestPath(i,j,k) = min{shortestPath(i,j,k − 1),shortestPath(i,k,k − 1) + shortestPath(k,j,k − 1)},
shortestPath(i,j,0) = edgeCost(i,j).
This formula is the heart of Floyd–Warshall. The algorithm works by first computing shortestPath(i, j, k) for all (i, j) pairs for k = 1, then k = 2, etc. This process continues until k = n, and we have found the shortest path for all (i, j) pairs using any intermediate vertices.
[edit]Pseudocode

Conveniently, when calculating the kth case, one can overwrite the information saved from the computation of k − 1. This means the algorithm uses quadratic memory. Be careful to note the initialization conditions:
 1 * Assume a function edgeCost(i,j) which returns the cost of the edge from i to j
 2    (infinity if there is none).
 3    Also assume that n is the number of vertices and edgeCost(i,i) = 0
 4 *
 5
 6 int path[][];
 7 * A 2-dimensional matrix. At each step in the algorithm, path[i][j] is the shortest path
 8    from i to j using intermediate vertices (1..k−1).  Each path[i][j] is initialized to
 9    edgeCost(i,j) or infinity if there is no edge between i and j.
10 *
11
12 procedure FloydWarshall ()
13    for k := 1 to n
14       for i := 1 to n
15          for j := 1 to n
16             path[i][j] = min ( path[i][j], path[i][k]+path[k][j] );
[edit]Behaviour with negative cycles

For numerically meaningful output, Floyd–Warshall assumes that there are no negative cycles (in fact, between any pair of vertices which form part of a negative cycle, the shortest path is not well-defined because the path can be arbitrarily negative). Nevertheless, if there are negative cycles, Floyd–Warshall can be used to detect them. The intuition is as follows:
The Floyd–Warshall algorithm iteratively revises path lengths between all pairs of verticies (i, j), including where i = j;
Initially, the length of the path (i,i) is zero;
A path {(i,k), (k,i)} can only improve upon this if it has length less than zero, i.e. denotes a negative cycle;
Thus, after the algorithm, (i,i) will be negative if there exists a negative-length path from i back to i.
Hence, to detect negative cycles using the Floyd–Warshall, one can inspect the diagonal of the path matrix, and the presence of a negative number indicates that the graph contains at least one negative cycle.[2]
[edit]Path reconstruction

The Floyd–Warshall algorithm typically only provides the lengths of the paths between all pairs of nodes. With simple modifications, it is possible to create a method to reconstruct the actual path between any two endpoint nodes. While one may be inclined to store the actual path from each node to each other node, this is not necessary, and in fact, is very costly in terms of memory. For each node, one need only store the information about which node one has to go through if he wishes to end up at any given node. Therefore, information to reconstruct all paths can be stored in an single N×N matrix 'next' where next[i][j] represents the node one must travel through if he intends to take the shortest path from i to j. Implementing such a scheme is trivial as when a new shortest path is found between two nodes, the matrix containing the paths is updated. The next matrix is updated along with the path matrix such that at completion both tables are complete and accurate, and any entries which are infinite in the path table will be null in the next table. The path from i to j is then path from i to path[i][j], followed by path from path[i][j] to j. These two shorter paths are determined recursively. This modified algorithm runs with the same time and space complexity as the unmodified algorithm.
 1 procedure FloydWarshallWithPathReconstruction ()
 2    for k := 1 to n
 3       for i := 1 to n
 4          for j := 1 to n
 5             if path[i][k] + path[k][j] < path[i][j]
 6                path[i][j] = path[i][k]+path[k][j];
 7                next[i][j] = k;
 8
 9 procedure GetPath (i,j)
10    if path[i][j] equals infinity then
11      return "no path";
12    int intermediate := next[i][j];
13    if intermediate equals 'null' then
14      return " ";   * there is an edge from i to j, with no vertices between *
15   else
16      return GetPath(i,intermediate) + intermediate + GetPath(intermediate,j);
[edit]Analysis

To find all n2 of shortestPath(i,j,k) (for all i and j) from those of shortestPath(i,j,k−1) requires 2n2 operations. Since we begin with shortestPath(i,j,0) = edgeCost(i,j) and compute the sequence of n matrices shortestPath(i,j,1), shortestPath(i,j,2), …, shortestPath(i,j,n), the total number of operations used is n · 2n2 = 2n3. Therefore, the complexity of the algorithm is Θ(n3) and can be solved by a deterministic machine in polynomial time.
         * 
         * 
         */
    }
}
