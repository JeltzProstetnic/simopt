using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Interfaces;
using System.Runtime.Serialization;

namespace SimOpt.Basics.Datastructures.Geometry
{
    [Serializable]
    public class Point<T> : IPoint3D<T>, IEquatable<Point<T>>
    {
        #region over

        public override string ToString()
        {
            if (Z.Equals(default(T)))
            {
                return "(" + X.ToString() + "," + Y.ToString() + ")";
            }
            else
            {
                return "(" + X.ToString() + "," + Y.ToString() + "," + Z.ToString() + ")";
            }
        }

        #endregion
        #region prop

        public T X { get; set; }
        public T Y { get; set; }
        public T Z { get; set; }

        #endregion
        #region ctor

        /// <summary>
        /// Default constructor will set all coordinates to NaN.
        /// </summary>
        public Point()
        {
            X = default(T);
            Y = default(T);
            Z = default(T);
        }

        /// <summary>
        /// convenience constructor, will set z to NaN
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Point(T x, T y)
        {
            X = x;
            Y = y;
            Z = default(T);
        }

        /// <summary>
        /// convenience constructor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Point(T x, T y, T z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        #endregion
        #region impl

        public bool Equals(Point<T> other)
        {
            if (other == null) return false;
            return (this.X.Equals(other.X) && this.Y.Equals(other.Y) &&
                (this.Z.Equals(other.Z)));
        }

        public override bool Equals(object obj)
        {
            if ((obj as Point<T>) == null) return false;
            return this.Equals(obj as Point<T>);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }

    [Serializable]
    public class Point : Point<double>, IPoint3D<double>, IEquatable<Point>, ISerializableSimulation
    {
        #region over

        public override string ToString()
        {
            if (double.IsNaN(Z))
            {
                return "(" + X.ToString() + "," + Y.ToString() + ")";
            }
            else
            {
                return "(" + X.ToString() + "," + Y.ToString() + "," + Z.ToString() + ")";
            }
        }

        #endregion
        #region stat

        public static Vector GetVector(Point from, Point to)
        {
            return to - from;
        }

        #endregion
        #region ctor

        /// <summary>
        /// Default constructor will set all coordinates to NaN.
        /// </summary>
        public Point()
        {
            X = double.NaN;
            Y = double.NaN;
            Z = double.NaN;
        }

        /// <summary>
        /// convenience constructor, will set z to NaN
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Point(double x = 0, double y = 0)
        {
            X = x;
            Y = y;
            Z = double.NaN;
        }

        /// <summary>
        /// convenience constructor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Point(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        #endregion
        #region oper

        public static bool operator ==(Point p1, Point p2)
        {
            if (object.Equals(p1, null)) return object.Equals(p2, null);
            return p1.Equals(p2);
        }

        public static bool operator !=(Point p1, Point p2)
        {
            return !(p1 == p2);
        }

        public static Point operator +(Point p1, Point p2)
        {
            return new Point(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
        }

        public static Point operator -(Point p1, Point p2)
        {
            return new Point(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }

        public static Point operator -(Point p1)
        {
            return new Point(-p1.X, -p1.Y, -p1.Z);
        }

        public static Point operator +(Point p1, Vector p2)
        {
            return new Point(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
        }

        public static Point operator -(Point p1, Vector p2)
        {
            return new Point(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }

        public static implicit operator Vector(Point point)
        {
            return new Vector(point.X, point.Y, point.Z);
        }


        #endregion
        #region impl

        public Vector ToVector()
        {
            return this;
        }

        /// <summary>
        /// returns the euclidean distance between this and the given point
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public double GetEuclideanDistance(Point other)
        {
            if (Double.IsNaN(Z) && Double.IsNaN(other.Z))
                return Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
            else if (!Double.IsNaN(Z) && !Double.IsNaN(other.Z))
                return Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2) + Math.Pow(Z - other.Z, 2));
            else
                throw new ArgumentException("Incompatible points: one of the point has a Z value defined, the other not.");
        }

        public bool Equals(Point other)
        {
            if (other == null) return false;
            return (this.X == other.X && this.Y == other.Y &&
                (this.Z == other.Z || (double.IsNaN(this.Z) && double.IsNaN(other.Z))));
        }

        public override bool Equals(object obj)
        {
            if ((obj as Point) == null) return false;
            return this.Equals(obj as Point);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #region ISerializableGrubi

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("X", X);
            info.AddValue("Y", Y);
            info.AddValue("Z", Z);
        }

        #endregion

        #endregion
    }
}
