///////////////////////////////////////////////////////////////////////////////////////
//
//    Project:     BlueLogic.SDelta
//    Description: Trace as in Myers Text Delta Algorithm
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
//      Mittwoch, 30. Mai  2007 Matthias Gruber moved, final testing & comments
//
//
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;
using System.Text;

namespace MatthiasToolbox.Delta.Diff
{
    /// <summary>
    /// this struct holds a collection of match points on a shortest edit script
    /// see MyersDiff.cs
    /// </summary>
    public struct Trace
    {
        #region over

        /// <summary>
        /// compares this instance to the given one
        /// </summary>
        /// <param name="obj">instance to compare</param>
        /// <returns>
        /// true if the instances match
        /// </returns>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        /// returns a non secure hash code for this instance
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// comparer overload
        /// </summary>
        /// <param name="a">instance 1</param>
        /// <param name="b">instance 2</param>
        /// <returns>true if the given traces match</returns>
        public static bool operator ==(Trace a, Trace b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// comparer overload
        /// </summary>
        /// <param name="a">instance 1</param>
        /// <param name="b">instance 2</param>
        /// <returns>false if the given traces match</returns>
        public static bool operator !=(Trace a, Trace b)
        {
            return !a.Equals(b);
        }

        #endregion
        #region cvar
        
        private int _count;     // private increment
        private Point[] _matchPoints;  // array of points
        
        #endregion
        #region prop
        
        /// <summary>
        /// contains match points used in a shortest edit script
        /// </summary>
        public Point[] MatchPoints()
        {
            return _matchPoints;
        }
        
        /// <summary>
        /// number of actually stored match points
        /// </summary>
        public int Count
        {
            get { return _count; }
        }
        
        #endregion
        #region ctor
        
        /// <summary>
        /// constructor for a known maximum of possible match points 
        /// </summary>
        /// <param name="size">
        /// the expected maximum number of match points
        /// set this to M + N to avoid out of bounds
        /// </param>
        public Trace(int size)
        {
            _matchPoints = new Point[size];
            _count = 0;
        }
        
        #endregion
        #region impl
        
        /// <summary>
        /// add a match point to the internal array
        /// </summary>
        /// <param name="x">
        /// 1 based position in source data 
        /// </param>
        /// <param name="y">
        /// 1 based position in target data
        /// </param>
        public void Add(int x, int y)
        {
            _matchPoints[_count] = new Point(x, y);
            _count += 1;
        }
        
        /// <summary>
        /// custom string cast
        /// </summary>
        /// <returns></returns>
        public new String ToString()
        {
            StringBuilder sb = new StringBuilder(_count);
            for (int i = 0; i < _count; i++)
            {
                sb.Append(_matchPoints[i].ToString());
            }
            return sb.ToString();
        }
        
        #endregion
    } // struct
} // namespace
