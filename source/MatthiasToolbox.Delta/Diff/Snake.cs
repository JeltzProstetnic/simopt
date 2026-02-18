using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Delta.Diff
{
    /// <summary>
    /// contains a snake as in the Myers paper
    /// </summary>
    public struct Snake
    {
        public int x;   // start position in source data
        public int y;   // start position in target data
        public int u;   // end position in source data
        public int v;   // end position in target data

        /// <summary>
        /// length of the snake
        /// </summary>
        /// <returns>
        /// number of diagonal moves
        /// </returns>
        public int Length()
        {
            return u - x;
        }

        /// <summary>
        /// Adds an offset to the contained points
        /// </summary>
        /// <param name="horizontal">horizontal offset</param>
        /// <param name="vertical">vertical offset</param>
        public void Add(int horizontal, int vertical)
        {
            x += horizontal;
            u += horizontal;
            y += vertical;
            v += vertical;
        }
    }
}