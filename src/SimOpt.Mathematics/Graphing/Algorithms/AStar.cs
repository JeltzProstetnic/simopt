using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Mathematics.Graphing.Algorithms
{
    [Serializable]
    public class AStar
    {
        /*
         * 
         * In computer science, A* (pronounced "A star") is a best-first graph search algorithm that finds the least-cost path from a given initial node to one goal node (out of one or more possible goals).
It uses a distance-plus-cost heuristic function (usually denoted f(x)) to determine the order in which the search visits nodes in the tree. The distance-plus-cost heuristic is a sum of two functions:
the path-cost function, which is the cost from the starting node to the current node (usually denoted g(x))
and an admissible "heuristic estimate" of the distance to the goal (usually denoted h(x)).
The h(x) part of the f(x) function must be an admissible heuristic; that is, it must not overestimate the distance to the goal. Thus for an application like routing, h(x) might represent the straight-line distance to the goal, since that is physically the smallest possible distance between any two points (or nodes for that matter).
If the heuristic h satisfies the additional condition  for every edge x, y of the graph (d denoting the length of that edge) then h is called monotone or consistent. In this case A* can be implemented more efficiently — roughly speaking, no node needs to be processed more than once (see closed set below) — and in fact A* is equivalent to running Dijkstra's algorithm with the reduced cost d'(x,y): = d(x,y) − h(x) + h(y).
The algorithm was first described in 1968 by Peter Hart, Nils Nilsson, and Bertram Raphael[1].
This algorithm has been generalized into a bidirectional heuristic search algorithm; see bidirectional search.
         * 
         * 
         * Like all informed search algorithms, it first searches the routes that appear to be most likely to lead towards the goal. What sets A* apart from a greedy best-first search is that it also takes the distance already traveled into account (the g(x) part of the heuristic is the cost from the start, and not simply the local cost from the previously expanded node).
The algorithm traverses various paths from start to goal. For each node x traversed, it maintains 3 values:
g(x): the actual shortest distance traveled from initial node to current node
h(x): the estimated (or "heuristic") distance from current node to goal
f(x): the sum of g(x) and h(x)
Starting with the initial node, it maintains a priority queue of nodes to be traversed, known as the open set (not to be confused with open sets in topology). The lower f(x) for a given node x, the higher its priority. At each step of the algorithm, the node with the lowest f(x) value is removed from the queue, the f and h values of its neighbors are updated accordingly, and these neighbors are added to the queue. The algorithm continues until a goal node has a lower f value than any node in the queue (or until the queue is empty). (Goal nodes may be passed over multiple times if there remain other nodes with lower f values, as they may lead to a shorter path to a goal.) The f value of the goal is then the length of the shortest path, since h at the goal is zero in an admissible heuristic. If the actual shortest path is desired, the algorithm may also update each neighbor with its immediate predecessor in the best path found so far; this information can then be used to reconstruct the path by working backwards from the goal node. Additionally, if the heuristic is monotonic (or consistent, see below), a closed set of nodes already traversed may be used to make the search more efficient.
[edit]Pseudo code

 function A*(start,goal)
     closedset := the empty set                 % The set of nodes already evaluated.     
     openset := set containing the initial node % The set of tentative nodes to be evaluated.
     g_score[start] := 0                        % Distance from start along optimal path.
     h_score[start] := heuristic_estimate_of_distance(start, goal)
     f_score[start] := h_score[start]           % Estimated total distance from start to goal through y.
     while openset is not empty
         x := the node in openset having the lowest f_score[] value
         if x = goal
             return reconstruct_path(came_from,goal)
         remove x from openset
         add x to closedset
         foreach y in neighbor_nodes(x)
             if y in closedset
                 continue
             tentative_g_score := g_score[x] + dist_between(x,y)
 
             if y not in openset
                 add y to openset
 
                 tentative_is_better := true
             elseif tentative_g_score < g_score[y]
                 tentative_is_better := true
             else
                 tentative_is_better := false
             if tentative_is_better = true
                 came_from[y] := x
                 g_score[y] := tentative_g_score
                 h_score[y] := heuristic_estimate_of_distance(y, goal)
                 f_score[y] := g_score[y] + h_score[y]
     return failure
 
 function reconstruct_path(came_from,current_node)
     if came_from[current_node] is set
         p = reconstruct_path(came_from,came_from[current_node])
         return (p + current_node)
     else
         return the empty path
The closed set can be omitted (yielding a tree search algorithm) if a solution is guaranteed to exist, or if the algorithm is adapted so that new nodes are added to the open set only if they have a lower f value than at any previous iteration.
         * 
         * Like breadth-first search, A* is complete in the sense that it will always find a solution if there is one.
If the heuristic function h is admissible, meaning that it never overestimates the actual minimal cost of reaching the goal, then A* is itself admissible (or optimal) if we do not use a closed set. If a closed set is used, then h must also be monotonic (or consistent) for A* to be optimal.
         * 
         * 
         * 
         * Implementation details
There are a number of simple optimizations or implementation details that can significantly affect the performance of an A* implementation. The first detail to note is that the way the priority queue handles ties can have a significant effect on performance in some situations. If ties are broken so the queue behaves in a LIFO manner, A* will behave like depth-first search among equal cost paths. If ties are broken so the queue behaves in a FIFO manner, A* will behave like breadth-first search among equal cost paths.
When a path is required at the end of the search, it is common to keep with each node a reference to that node's parent. At the end of the search these references can be used to recover the optimal path. If these references are being kept then it can be important that the same node doesn't appear in the priority queue more than once (each entry corresponding to a different path to the node, and each with a different cost). A standard approach here is to check if a node about to be added already appears in the priority queue. If it does, then the priority and parent pointers are changed to correspond to the lower cost path. When finding a node in a queue to perform this check, many standard implementations of a min-heap require O(n) time. Augmenting the heap with a hash table can reduce this to constant time.
         * 
         *
         * The time complexity of A* depends on the heuristic. In the worst case, the number of nodes expanded is exponential in the length of the solution (the shortest path), but it is polynomial when the search space is a tree, there is a single goal state, and the heuristic function h meets the following condition:
| h(x) − h * (x) | = O(logh * (x))
where h * is the optimal heuristic, i.e. the exact cost to get from x to the goal. In other words, the error of h should not grow faster than the logarithm of the “perfect heuristic” h * that returns the true distance from x to the goal (see Pearl 1984[4] and also Russell and Norvig 2003, p. 101[5])
More problematic than its time complexity is A*’s memory usage. In the worst case, it must also remember an exponential number of nodes. Several variants of A* have been developed to cope with this, including iterative deepening A* (IDA*), memory-bounded A* (MA*) and simplified memory bounded A* (SMA*) and recursive best-first search (RBFS).
         * 
         */
    }
}
