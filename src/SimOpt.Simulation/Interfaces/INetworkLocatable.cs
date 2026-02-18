using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Simulation.Interfaces
{
    interface INetworkLocatable
    {
        ///// <summary>
        ///// the network on which the instance is located
        ///// </summary>
        //TNetwork Network { get; set; }

        ///// <summary>
        ///// the current node of the instance
        ///// </summary>
        //TNode CurrentNode { get; set; }

        ///// <summary>
        ///// the original node of this instance. on reset
        ///// the current node will be set to this
        ///// </summary>
        //TNode HomeNode { get; set; }

        // -movable:

        ///// <summary>
        ///// the current position
        ///// </summary>
        //Point3F CurrentPosition { get; }

        ///// <summary>
        ///// the current path if the object is on a path at the moment
        ///// </summary>
        //TPath CurrentPath { get; set; }

        ///// <summary>
        ///// the current position on the path if 
        ///// the instance is currently on a path
        ///// </summary>
        //double CurrentPositionOnPath { get; }

        ///// <summary>
        ///// orders the object to drive from "fromNode" to 
        ///// "toNode" using the given path
        ///// </summary>
        ///// <param name="fromNode"></param>
        ///// <param name="toNode"></param>
        ///// <param name="connectingPath"></param>
        ///// <returns></returns>
        //double Drive(TNode fromNode, TNode toNode, TPath connectingPath);
    }
}
