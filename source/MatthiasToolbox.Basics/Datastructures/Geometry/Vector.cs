using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Basics.Datastructures.Geometry
{
    [Serializable]
    public class Vector : IEquatable<Vector>
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

        /// <summary>
        /// Calculate the unit vector of this instance.
        /// </summary>
        /// <returns>A new vector of length 1</returns>
        public static Vector Normalize(Vector source)
        {
            double l = source.Length();
            return new Vector(source.X / l, source.Y / l, source.Z / l);
        }

        /// <summary>
        /// create a vector from the given points
        /// </summary>
        /// <param name="fromPoint"></param>
        /// <param name="toPoint"></param>
        /// <returns></returns>
        public static Vector Create(Point fromPoint, Point toPoint)
        {
            return (Vector)(toPoint - fromPoint);
        }

        #endregion
        #region prop

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        #endregion
        #region ctor

        /// <summary>
        /// Default constructor will set all coordinates to NaN.
        /// </summary>
        public Vector()
        {
            X = double.NaN;
            Y = double.NaN;
            Z = double.NaN;
        }

        /// <summary>
        /// Convenience constructor.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Vector(double x = 0, double y = 0)
        {
            X = x;
            Y = y;
            Z = double.NaN;
        }

        /// <summary>
        /// Convenience constructor.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Vector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        #endregion
        #region oper

        public static bool operator ==(Vector p1, Vector p2)
        {
            if (object.Equals(p1, null)) return object.Equals(p2, null);
            return p1.Equals(p2);
        }

        public static bool operator !=(Vector p1, Vector p2)
        {
            return !(p1 == p2);
        }

        public static Vector operator +(Vector p1, Vector p2)
        {
            return new Vector(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
        }

        public static Vector operator -(Vector p1, Vector p2)
        {
            return new Vector(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }

        public static Vector operator -(Vector p1)
        {
            return new Vector(-p1.X, -p1.Y, -p1.Z);
        }

        public static Vector operator *(Vector p1, double d)
        {
            return new Vector(p1.X * d, p1.Y * d, p1.Z * d);
        }

        public static Vector operator *(double d, Vector p1)
        {
            return new Vector(p1.X * d, p1.Y * d, p1.Z * d);
        }

        public static implicit operator Point(Vector v)
        {
            return new Point(v.X, v.Y, v.Z);
        }

        /// <summary>
        /// CAUTION: this is the DOT Product
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static double operator *(Vector v1, Vector v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        /// <summary>
        /// this is the CROSS Product
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Vector operator ^(Vector v1, Vector v2)
        {
            return new Vector(v1.Y * v2.Z - v1.Z * v2.Y, v1.Z * v2.X - v1.X * v2.Z, v1.X * v2.Y - v1.Y * v2.X);
        }

        #endregion
        #region impl

        /// <summary>
        /// calculate the length of this vector using the pythagorean formula
        /// </summary>
        /// <returns></returns>
        public double Length()
        {
            if (double.IsNaN(Z)) return Math.Sqrt(X * X + Y * Y);
            else return Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        /// <summary>
        /// 3D Rotation around a given axis (will be normalized to length 1)
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="angle"></param>
        public void Rotate(Vector axis, double angle)
        {
            if (axis.Length() != 1) axis.Normalize();
            Vector result = this * Math.Cos(angle) + (this ^ axis) * Math.Sin(angle) + axis * (axis * this) * (1 - Math.Cos(angle));
            this.X = result.X;
            this.Y = result.Y;
            this.Z = result.Z;
        }

        /// <summary>
        /// Two dimensional rotation (around Z axis)
        /// </summary>
        /// <param name="angle"></param>
        public void Rotate(double angle)
        {
            double x = X * Math.Cos(angle) - Y * Math.Sin(angle);
            double y = X * Math.Sin(angle) + Y * Math.Cos(angle);
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        /// 3D Rotation around a given axis (will be normalized to length 1)
        /// </summary>
        /// <param name="original"></param>
        /// <param name="axis"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Vector Rotate(Vector original, Vector axis, double angle)
        {
            if (axis.Length() != 1) axis.Normalize();
            return original * Math.Cos(angle) + (original ^ axis) * Math.Sin(angle) + axis * (axis * original) * (1 - Math.Cos(angle));
        }

        /// <summary>
        /// Two dimensional rotation (around Z axis)
        /// </summary>
        /// <param name="original"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Vector Rotate(double angle, Vector original)
        {
            return new Vector(original.X * Math.Cos(angle) - original.Y * Math.Sin(angle), original.X * Math.Sin(angle) + original.Y * Math.Cos(angle));
        }

        public void Normalize()
        {
            double len = Length();
            X /= len;
            Y /= len;
            if (!double.IsNaN(Z)) Z /= len;
        }

        public static Vector AsNormalized(Vector source)
        {
            double len = source.Length();

            if (double.IsNaN(source.Z))
                return new Vector(source.X / len, source.Y / len);
            else
                return new Vector(source.X / len, source.Y / len, source.Z / len);
        }

        public bool Equals(Vector other)
        {
            if (other == null) return false;
            return (this.X == other.X && this.Y == other.Y &&
                (this.Z == other.Z || (double.IsNaN(this.Z) && double.IsNaN(this.Z))));
        }

        public override bool Equals(object obj)
        {
            if ((obj as Vector) == null) return false;
            return this.Equals(obj as Vector);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }
}
