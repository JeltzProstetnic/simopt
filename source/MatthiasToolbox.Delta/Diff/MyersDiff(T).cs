///////////////////////////////////////////////////////////////////////////////////////
//
//    Project:     BlueLogic.SDelta
//    Description: Myers Text Delta Algorithm incl. non generic wrapper
//    Status:      RELEASE
//
// ------------------------------------------------------------------------------------
//    Copyright 2007 by Bluelogic Software Solutions.
//    see product licence ( creative commons attribution 3.0 )
// ------------------------------------------------------------------------------------
//
//    History:
//
//    Donnerstag, 29. März 2007 Matthias Gruber original version
//      Dienstag, 08. Mai  2007 Matthias Gruber first working version
//       Samstag, 12. Mai  2007 Matthias Gruber major refractoring
//       Sonntag, 13. Mai  2007 Matthias Gruber further refractoring, unit tests
//      Dienstag, 29. Mai  2007 Matthias Gruber final testing & comments
//
//
///////////////////////////////////////////////////////////////////////////////////////

using System;
using Trace = MatthiasToolbox.Delta.Diff.Trace;

namespace MatthiasToolbox.Delta.Diff
{
    /// <summary>
    /// this class is an implementation of the algorithm published in
    /// "An O(ND) Difference Algorithm and its Variations" by Eugene Myers
    /// Algorithmica Vol. 1 No. 2, 1986, p 251.
    /// it is generically modified to be able to compare not only chars
    /// but also any other IComparable types
    /// further modifications to the original description are indicated
    /// </summary>
    /// <typeparam name="T">
    /// type of the array items to be compared
    /// </typeparam>
    public class MyersDiff<T> where T : IComparable
    {
        #region cvar

        private Snake mySnake;  // a temporary snake to save intermediary results
        private Trace myTrace;  // a temporary trace to save the results
        
        /// <summary>
        /// these are the temporary variables for the original algorithm
        /// the not-so-speaking names are due to the intention of making
        /// this code easily comparable to the pseudo codes in the original paper
        /// </summary>
        private int D;
        private int M;
        private int N;
        private int k;
        private int maxEndpoints;
        private int[] Vf;
        private int[] Vb;
        private int x;
        private int y;
        private int u;
        private int v;
        private T[] a;
        private T[] b;
        private int delta;
        private int maxD;

        /// <summary>
        /// additionaly needed temporary variables
        /// these are designed as classvars to avoid instantiations 
        /// within the algorithm itself for performance reasons
        /// </summary>
        private bool odd;
        private int aOffset;    // offsets for the data arrays, which would have to be copied otherwise (as in the original paper) 
        private int bOffset;
        private int kNull;      // offset for the forward and backward arrays which use negative indices in the original paper
        private int len;        // length of the found shortest edit script
        private int i;

        #endregion
        #region prop
        
        /// <summary>
        /// result of the algorithm containing the traversed match points in the found edit script
        /// </summary>
        public Trace Trace
        {
            get { return myTrace; }
        } // Trace
        
        #endregion
        #region ctor

        /// <summary>
        /// initialize the algorithm
        /// </summary>
        /// <param name="a">
        /// source data
        /// </param>
        /// <param name="b">
        /// target data
        /// </param>
        public MyersDiff(T[] a, T[] b)
        {
            this.a = a;
            this.b = b;
            mySnake = new Snake();
            myTrace = new Trace(Math.Min(a.Length, b.Length));
        } // void
        
        #endregion
        #region impl

        #region public

        /// <summary>
        /// recursivly calclate the shortest edit script and store the trace
        /// in contrast to the original paper we need the explicit path which can be
        /// reconstructed by the traversed match points
        /// (C) Matthias Gruber
        /// </summary>
        /// <returns>
        /// length of the shortest edit script
        /// </returns>
        public int MakeEditScript()
        {
            return getEditScript(0, 0, a.Length, b.Length);
        } // int

        /// <summary>
        /// recursivly calclate the length of the shortest edit script 
        /// (C) Matthias Gruber
        /// </summary>
        /// <returns></returns>
        public int CalculateLength()
        {
            return calculateLength(0, 0, a.Length, b.Length);
        } // int

        #endregion
        #region private

        /// <summary>
        /// find the shortest edit script
        /// </summary>
        /// <returns>
        /// length of the edit script
        /// </returns>
        private int SES()
        {
            delta = N - M;
            odd = (delta & 1) != 0;
            if(odd) return oddSES(); 
            else return evenSES();
        } // int
        
        /// <summary>
        /// find the shortest edit script in case of odd N - M
        /// </summary>
        /// <returns>
        /// length of shortest edit script
        /// </returns>
        private int oddSES()
        {
            // initialize starting positions
            Vf[1 + kNull] = 0;
            Vb[-1 + kNull] = N;
            
            for(D = 0; D <= maxD; D += 1)
            {
                // forward search
                for(k = -D; k <= D; k += 2)
                {
                    findForward();
                    if(k >= delta - (D - 1) && k <= delta + (D - 1))
                    {
                        if(checkOverlapF()) return 2 * D - 1;
                    }
                }

                // backward search
                for (k = -D; k <= D; k += 2)
                {
                    findBack();
                }
            }
            
            // this should never happen:
            return -1;
        } // int

        /// <summary>
        /// find the shortest edit script in case of even N - M
        /// </summary>
        /// <returns>
        /// length of shortest edit script
        /// </returns>
        private int evenSES()
        {
            // initialize starting positions
            Vf[1 + kNull] = 0;
            Vb[-1 + kNull] = N;
            
            for (D = 0; D <= maxD; D += 1)
            {
                // forward search
                for (k = -D; k <= D; k += 2)
                {
                    findForward();
                }

                // backward search
                for (k = -D; k <= D; k += 2)
                {
                    findBack();
                    if (k + delta >= -D && k + delta <= D)
                    {
                        if(checkOverlapB()) return 2 * D;
                    }
                }
            }
            
            // this should never happen:
            return -1;
        } // int
        
        /// <summary>
        /// find end of furthest reaching reverse D path in diagonal k + delta
        /// </summary>
        private void findBack()
        {
            // get starting position
            if (k == D || k != -D && Vb[k + 1 + kNull] > Vb[k - 1 + kNull])
            {
                u = Vb[k - 1 + kNull];
            }
            else
            {
                u = Vb[k + 1 + kNull] - 1;
            }
            
            v = u - k - delta;

            // save beginning of snake
            mySnake.u = u;
            mySnake.v = v;
            
            // traverse snake
            while (u > 0 && v > 0 && a[u - 1 + aOffset].CompareTo(b[v - 1 + bOffset]) == 0)
            {
                u -= 1;
                v -= 1;
            }
            
            // save end of snake
            mySnake.x = u;
            mySnake.y = v;
            
            // store new position
            Vb[k + kNull] = u;
            
        } // void

        /// <summary>
        /// find end of furthest reaching forward D path in diagonal k
        /// </summary>
        private void findForward()
        {
            // get start position
            if (k == -D || k != D && Vf[k - 1 + kNull] < Vf[k + 1 + kNull])
            {
                x = Vf[k + 1 + kNull];
            }
            else
            {
                x = Vf[k - 1 + kNull] + 1;
            }
            
            y = x - k;

            // save beginning of snake
            mySnake.x = x;
            mySnake.y = y;
            
            // traverse snake
            while (x < N && y < M && a[x + aOffset].CompareTo(b[y + bOffset]) == 0)
            {
                x += 1;
                y += 1;
            }
            
            // save end of snake
            mySnake.u = x;
            mySnake.v = y;
            
            // save new position
            Vf[k + kNull] = x;
        } // void
        
        /// <summary>
        /// check overlap in forward search
        /// </summary>
        /// <returns>
        /// boolean indicating if the paths overlap
        /// </returns>
        private bool checkOverlapF()
        {
            return (Vf[k + kNull] >= Vb[k - delta + kNull]);
        } // bool

        /// <summary>
        /// check overlap in backward search
        /// </summary>
        /// <returns>
        /// boolean indicating if the paths overlap
        /// </returns>
        private bool checkOverlapB()
        {
            return (Vf[k + delta + kNull] >= Vb[k + kNull]);
        } // bool

        /// <summary>
        /// recursivly calclate the shortest edit script and store the trace
        /// in contrast to the original paper we need the explicit path which can be
        /// reconstructed by the traversed match points
        /// (C) Matthias Gruber
        /// </summary>
        /// <param name="x1">
        /// start position in source data
        /// </param>
        /// <param name="y1">
        /// start position in target data
        /// </param>
        /// <param name="x2">
        /// end position in source data
        /// </param>
        /// <param name="y2">
        /// end position in target data
        /// </param>
        /// <returns>
        /// length of the shortest edit script
        /// </returns>
        private int getEditScript(int x1, int y1, int x2, int y2)
        {
            N = x2 - x1;
            M = y2 - y1;
            if (M <= 0 || N <= 0) return 0;
            Snake result;
            aOffset = x1;
            bOffset = y1;
            maxEndpoints = 2 * (M + N) + 1;
            kNull = M + N;
            Vf = new int[maxEndpoints];
            Vb = new int[maxEndpoints];
            maxD = ((M + N) / 2) + 1;
            
            len = SES();
            result = mySnake;
            result.Add(aOffset, bOffset);
        
            if(len > 1) // 1+path
            {
                getEditScript(x1, y1, result.x, result.y);
                for (i = 0; i < result.Length(); i++)
                {
                    myTrace.Add(result.x + i, result.y + i);
                } // for
                getEditScript(result.u, result.v, x2, y2);
            } // if
            else if(M > N) // 0-1-path, insert
            {
                int bIndex = y1;
                for (int aIndex = x1; aIndex < x2; aIndex++)
                {
                    while (bIndex < y2 && b[bIndex].CompareTo(a[aIndex]) != 0) bIndex+=1;
                    if (bIndex < y2 && b[bIndex].CompareTo(a[aIndex]) == 0) myTrace.Add(aIndex, bIndex);
                    bIndex += 1;
                }
            } // else if
            else // 0-1-path, delete
            {
                int aIndex = x1;
                for (int bIndex = y1; bIndex < y2; bIndex++)
                {
                    while (aIndex < x2 && a[aIndex].CompareTo(b[bIndex]) != 0) aIndex+=1;
                    if (aIndex < x2 && a[aIndex].CompareTo(b[bIndex]) == 0) myTrace.Add(aIndex, bIndex);
                    aIndex += 1;
                }
            } // else
            
            return len;
        } // int

        /// <summary>
        /// recursivly calclate the length of the shortest edit script 
        /// (C) Matthias Gruber
        /// </summary>
        /// <param name="x1">
        /// start position in source data
        /// </param>
        /// <param name="y1">
        /// start position in target data
        /// </param>
        /// <param name="x2">
        /// end position in source data
        /// </param>
        /// <param name="y2">
        /// end position in target data
        /// </param>
        /// <returns>
        /// length of the shortest edit script
        /// </returns>
        private int calculateLength(int x1, int y1, int x2, int y2)
        {
            N = x2 - x1;
            M = y2 - y1;
            if (M <= 0 || N <= 0) return 0;
            aOffset = x1;
            bOffset = y1;
            maxEndpoints = 2 * (M + N) + 1;
            kNull = M + N;
            Vf = new int[maxEndpoints];
            Vb = new int[maxEndpoints];
            maxD = ((M + N) / 2) + 1;

            return SES();
        } // int

        #endregion

        #endregion
    } // class
} // namespae