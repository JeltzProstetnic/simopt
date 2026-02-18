using System;
using System.Collections.Generic;
using System.Data;
using MatthiasToolbox.Mathematics.Numerics;
using MatthiasToolbox.Mathematics.Numerics.Decompositions;
using MatthiasToolbox.Mathematics.Utilities;

namespace MatthiasToolbox.Mathematics
{
    /// <summary>
    /// Special region from the Cephes Math Library, http://www.netlib.org/cephes/
    /// </summary>
    public static class MMath
    {
        #region cvar

        public static double HalfPi = Math.PI / 2;
        public static double TwoPi = Math.PI * 2;
        public static double QuarterPI = Math.PI / 4.0;
        public static double RTD = 180.0 / Math.PI;
        public static double DTR = Math.PI / 180.0;
        private const double Epsilon = 1.11022302462515654042E-16;
        private const double MaxLog = 7.09782712893383996732E2;
        private const double MinLog = -7.451332191019412076235E2;
        private const double MaxGamma = 171.624376956302725;
        private const double SqrtPi = 2.50662827463100050242E0;
        private const double SqrtH = 7.07106781186547524401E-1;
        private const double LogPi = 1.14472988584940017414;
        private const double Sqrt2 = 1.4142135623730950488016887;

        #endregion
        #region impl
        
        #region numeric

        /// <summary>
        /// Modulus operation for an integer x and modulo m with respect to negative numbers.
        /// </summary>
        public static int Mod(int x, int m)
        {
            if (m < 0) m = -m;
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        /// <summary>
        /// Returns the factorial of the specified value using gamma approx. for non integers.
        /// </summary>
        public static double Factorial(double value)
        {
            double d = System.Math.Abs(value);

            // Check if the number is an integer
            if (System.Math.Floor(d) == d)
            {
                // Calculate factorial iteratively
                return (double)Factorial((int)value);
            }
            else
            {
                // Return Gamma approximation
                return Gamma(value + 1.0);
            }
        }

        /// <summary>
        /// Returns the factorial of the specified value.
        /// </summary>
        public static int Factorial(int value)
        {
            int i = System.Math.Abs(value);
            int fac = 1;

            while (i > 1) fac *= i--;

            // return factorial with original signum
            return (value < 0) ? -fac : fac;
        }

        public static bool IsEven(this int i)
        {
            return (i % 2) == 0;
        }

        public static bool IsEven(this long i)
        {
            return (i % 2) == 0;
        }

        public static bool IsOdd(this int i)
        {
            return (i % 2) != 0;
        }

        public static bool IsOdd(this long i)
        {
            return (i % 2) != 0;
        }

        public static double FractionalPart(double v)
        {
            return v - Math.Truncate(v);
        }

        public static decimal FractionalPart(decimal v)
        {
            return v - Math.Truncate(v);
        }

        #endregion
        #region exponential

        /// <summary>
        /// Calculates power of 2.
        /// </summary>
        /// 
        /// <param name="power">Power to raise in.</param>
        /// 
        /// <returns>Returns specified power of 2 in the case if power is in the range of
        /// [0, 30]. Otherwise returns 0.</returns>
        /// 
        public static int Pow2(int power)
        {
            return ((power >= 0) && (power <= 30)) ? (1 << power) : 0;
        }

        /// <summary>
        /// Checks if the specified integer is power of 2.
        /// </summary>
        /// 
        /// <param name="x">Integer number to check.</param>
        /// 
        /// <returns>Returns <b>true</b> if the specified number is power of 2.
        /// Otherwise returns <b>false</b>.</returns>
        /// 
        public static bool IsPowerOf2(int x)
        {
            return (x & (x - 1)) == 0;
        }

        /// <summary>
        /// Get base of binary logarithm.
        /// </summary>
        /// 
        /// <param name="x">Source integer number.</param>
        /// 
        /// <returns>Power of the number (base of binary logarithm).</returns>
        /// 
        public static int Log2(int x)
        {
            if (x <= 65536)
            {
                if (x <= 256)
                {
                    if (x <= 16)
                    {
                        if (x <= 4)
                        {
                            if (x <= 2)
                            {
                                if (x <= 1)
                                    return 0;
                                return 1;
                            }
                            return 2;
                        }
                        if (x <= 8)
                            return 3;
                        return 4;
                    }
                    if (x <= 64)
                    {
                        if (x <= 32)
                            return 5;
                        return 6;
                    }
                    if (x <= 128)
                        return 7;
                    return 8;
                }
                if (x <= 4096)
                {
                    if (x <= 1024)
                    {
                        if (x <= 512)
                            return 9;
                        return 10;
                    }
                    if (x <= 2048)
                        return 11;
                    return 12;
                }
                if (x <= 16384)
                {
                    if (x <= 8192)
                        return 13;
                    return 14;
                }
                if (x <= 32768)
                    return 15;
                return 16;
            }

            if (x <= 16777216)
            {
                if (x <= 1048576)
                {
                    if (x <= 262144)
                    {
                        if (x <= 131072)
                            return 17;
                        return 18;
                    }
                    if (x <= 524288)
                        return 19;
                    return 20;
                }
                if (x <= 4194304)
                {
                    if (x <= 2097152)
                        return 21;
                    return 22;
                }
                if (x <= 8388608)
                    return 23;
                return 24;
            }
            if (x <= 268435456)
            {
                if (x <= 67108864)
                {
                    if (x <= 33554432)
                        return 25;
                    return 26;
                }
                if (x <= 134217728)
                    return 27;
                return 28;
            }
            if (x <= 1073741824)
            {
                if (x <= 536870912)
                    return 29;
                return 30;
            }
            return 31;
        }

        /// <summary>
        /// The falling power of the specified value.
        /// </summary>
        public static int Falling(int value, int power)
        {
            int t = 1;
            for (int i = 0; i < power; i++) t *= power--;
            return t;
        }

        /// <summary>
        /// Truncated power function.
        /// </summary>
        public static double TruncPower(double value, double degree)
        {
            double x = System.Math.Pow(value, degree);
            return (x > 0) ? x : 0.0;
        }

        #endregion
        #region trigonometric

        #region tools

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static double Hypotenuse(double x, double y)
        {
            if (x < 0.0) x = -x;
            else if (x == 0.0) return y < 0.0 ? -y : y;
            
            if (y < 0.0) y = -y;
            else if (y == 0.0) return x;

            if (x < y)
            {
                x /= y;
                return y * Math.Sqrt(1.0 + x * x);
            }
            else
            {
                y /= x;
                return x * Math.Sqrt(1.0 + y * y);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="angle">angle in radians</param>
        /// <returns></returns>
        public static double NormalizeAngle(double angle)
        {
            if (Double.IsInfinity(angle) || Double.IsNaN(angle)) throw new NotFiniteNumberException("Infinite angle");
            while (angle > TwoPi) angle -= TwoPi;
            while (angle < 0) angle += TwoPi;
            return angle;
        }

        #endregion
        #region trigonometrics in degree

        public static double SinDegree(double v)
        {
            return Math.Sin(v * DTR);
        }

        public static double CosDegree(double v)
        {
            return Math.Cos(v * DTR);
        }

        public static double TanDegree(double v)
        {
            return Math.Tan(v * DTR);
        }

        public static double AsinDegree(double v)
        {
            return Math.Asin(v) * RTD;
        }

        public static double AcosDegree(double v)
        {
            return Math.Acos(v) * RTD;
        }

        public static double AtanDegree(double v)
        {
            return Math.Atan(v) * RTD;
        }

        public static double Atan2Degree(double y, double x)
        {
            return Math.Atan2(y, x) * RTD;
        }

        #endregion
        #region arc functions

        /// <summary>
        /// The hyperbolic arc cosine of the specified value.
        /// </summary>
        public static double Acosh(double x)
        {
            if (x < 1.0)
                throw new ArgumentOutOfRangeException("x");
            return System.Math.Log(x + System.Math.Sqrt(x * x - 1));
        }

        /// <summary>
        /// The hyperbolic arc sine of the specified value.
        /// </summary>
        public static double Asinh(double d)
        {
            double x;
            int sign;

            if (d == 0.0)
                return d;

            if (d < 0.0)
            {
                sign = -1;
                x = -d;
            }
            else
            {
                sign = 1;
                x = d;
            }
            return sign * System.Math.Log(x + System.Math.Sqrt(x * x + 1));
        }

        /// <summary>
        /// The hyperbolic arc tangent of the specified value.
        /// </summary>
        public static double Atanh(double d)
        {
            if (d > 1.0 || d < -1.0)
                throw new ArgumentOutOfRangeException("d");
            return 0.5 * System.Math.Log((1.0 + d) / (1.0 - d));
        }

        #endregion

        #endregion
        #region geometry

        /// <summary>
        /// 
        /// </summary>
        /// <param name="angle">angle in radians</param>
        /// <returns></returns>
        public static double NormalizeLatitude(double angle)
        {
            if (Double.IsInfinity(angle) || Double.IsNaN(angle)) throw new NotFiniteNumberException("Infinite latitude.");
            while (angle > MMath.HalfPi) angle -= Math.PI;
            while (angle < -MMath.HalfPi) angle += Math.PI;
            return angle;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="angle">angle in radians</param>
        /// <returns></returns>
        public static double NormalizeLongitude(double angle)
        {
            if (Double.IsInfinity(angle) || Double.IsNaN(angle)) throw new NotFiniteNumberException("Infinite longitude.");
            while (angle > Math.PI) angle -= TwoPi;
            while (angle < -Math.PI) angle += TwoPi;
            return angle;
        }

        #endregion
        #region range functions

        /// <summary>
        ///   Converts the value x (which is measured in the scale
        ///   'from') to another value measured in the scale 'to'.
        /// </summary>
        public static int Scale(this RangeInteger from, RangeInteger to, int x)
        {
            if (from.Length == 0) return 0;
            return (to.Length) * (x - from.Min) / from.Length + to.Min;
        }

        /// <summary>
        ///   Converts the value x (which is measured in the scale
        ///   'from') to another value measured in the scale 'to'.
        /// </summary>
        public static double Scale(this RangeDouble from, RangeDouble to, double x)
        {
            if (from.Length == 0) return 0;
            return (to.Length) * (x - from.Min) / from.Length + to.Min;
        }

        /// <summary>
        ///   Converts the value x (which is measured in the scale
        ///   'from') to another value measured in the scale 'to'.
        /// </summary>
        public static double Scale(double fromMin, double fromMax, double toMin, double toMax, double x)
        {
            if (fromMax - fromMin == 0) return 0;
            return (toMax - toMin) * (x - fromMin) / (fromMax - fromMin) + toMin;
        }

        #endregion
        #region complex

        /// <summary>
        ///   Computes the absolute value of an array of complex numbers.
        /// </summary>
        public static Complex[] Abs(this Complex[] x)
        {
            Complex[] r = new Complex[x.Length];
            for (int i = 0; i < x.Length; i++)
                r[i] = new Complex(x[i].Magnitude, 0);
            return r;
        }

        /// <summary>
        ///   Computes the sum of an array of complex numbers.
        /// </summary>
        public static Complex Sum(this Complex[] x)
        {
            Complex r = Complex.Zero;
            for (int i = 0; i < x.Length; i++)
                r += x[i];
            return r;
        }

        /// <summary>
        ///   Elementwise multiplication of two complex vectors.
        /// </summary>
        public static Complex[] Multiply(this Complex[] a, Complex[] b)
        {
            Complex[] r = new Complex[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                r[i] = Complex.Multiply(a[i], b[i]);
            }
            return r;
        }

        /// <summary>
        ///   Gets the magnitude of every complex number in an array.
        /// </summary>
        public static double[] Magnitude(this Complex[] c)
        {
            double[] magnitudes = new double[c.Length];
            for (int i = 0; i < c.Length; i++)
                magnitudes[i] = c[i].Magnitude;

            return magnitudes;
        }

        /// <summary>
        ///   Gets the phase of every complex number in an array.
        /// </summary>
        public static double[] Phase(this Complex[] c)
        {
            double[] phases = new double[c.Length];
            for (int i = 0; i < c.Length; i++)
                phases[i] = c[i].Phase;

            return phases;
        }

        /// <summary>
        ///   Returns the real vector part of the complex vector c.
        /// </summary>
        /// <param name="c">A vector of complex numbers.</param>
        /// <returns>A vector of scalars with the real part of the complex numers.</returns>
        public static double[] Re(this Complex[] c)
        {
            double[] re = new double[c.Length];
            for (int i = 0; i < c.Length; i++)
                re[i] = c[i].Re;

            return re;
        }

        /// <summary>
        ///   Returns the imaginary vector part of the complex vector c.
        /// </summary>
        /// <param name="c">A vector of complex numbers.</param>
        /// <returns>A vector of scalars with the imaginary part of the complex numers.</returns>
        public static double[] Im(this Complex[] c)
        {
            double[] im = new double[c.Length];
            for (int i = 0; i < c.Length; i++)
                im[i] = c[i].Im;

            return im;
        }

        /// <summary>
        ///   Converts a complex number to a matrix of scalar values
        ///   in which the first column contains the real values and 
        ///   the second column contains the imaginary values.
        /// </summary>
        /// <param name="c">An array of complex numbers.</param>
        public static double[,] ToArray(this Complex[] c)
        {
            double[,] arr = new double[c.Length, 2];
            for (int i = 0; i < c.GetLength(0); i++)
            {
                arr[i, 0] = c[i].Re;
                arr[i, 1] = c[i].Im;
            }

            return arr;
        }

        /// <summary>
        ///   Gets the range of the magnitude values in a complex number vector.
        /// </summary>
        /// <param name="array">A complex number vector.</param>
        /// <returns>The range of magnitude values in the complex vector.</returns>
        public static RangeDouble Range(this Complex[] array)
        {
            double min = array[0].SquaredMagnitude;
            double max = array[0].SquaredMagnitude;

            for (int i = 1; i < array.Length; i++)
            {
                double sqMagnitude = array[i].SquaredMagnitude;
                if (min > sqMagnitude)
                    min = sqMagnitude;
                if (max < sqMagnitude)
                    max = sqMagnitude;
            }

            return new RangeDouble(
                System.Math.Sqrt(min),
                System.Math.Sqrt(max));
        }

        #endregion
        #region matrices

        #region comparison

        /// <summary>
        ///   Compares two matrices for equality, considering an acceptance threshold.
        /// </summary>
        public static bool IsEqual(this double[,] a, double[,] b, double threshold)
        {
            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < b.GetLength(1); j++)
                {
                    double x = a[i, j], y = b[i, j];

                    if (System.Math.Abs(x - y) > threshold || (Double.IsNaN(x) ^ Double.IsNaN(y)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        ///   Compares two matrices for equality, considering an acceptance threshold.
        /// </summary>
        public static bool IsEqual(this double[][] a, double[][] b, double threshold)
        {
            for (int i = 0; i < a.Length; i++)
            {
                for (int j = 0; j < a[i].Length; j++)
                {
                    double x = a[i][j], y = b[i][j];

                    if (System.Math.Abs(x - y) > threshold || (Double.IsNaN(x) ^ Double.IsNaN(y)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        ///   Compares two vectors for equality, considering an acceptance threshold.
        /// </summary>
        public static bool IsEqual(this double[] a, double[] b, double threshold)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (System.Math.Abs(a[i] - b[i]) > threshold)
                    return false;
            }
            return true;
        }

        /// <summary>
        ///   Compares each member of a vector for equality with a scalar value x.
        /// </summary>
        public static bool IsEqual(this double[] a, double x)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != x)
                    return false;
            }
            return true;
        }

        /// <summary>
        ///   Compares each member of a matrix for equality with a scalar value x.
        /// </summary>
        public static bool IsEqual(this double[,] a, double x)
        {
            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < a.GetLength(1); j++)
                {
                    if (a[i, j] != x)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        ///   Compares two matrices for equality.
        /// </summary>
        public static bool IsEqual<T>(this T[][] a, T[][] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                for (int j = 0; j < a[i].Length; j++)
                {
                    if (!a[i][j].Equals(b[i][j]))
                        return false;
                }
            }
            return true;
        }

        /// <summary>Compares two matrices for equality.</summary>
        public static bool IsEqual<T>(this T[,] a, T[,] b)
        {
            if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
                return false;

            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < b.GetLength(1); j++)
                {
                    if (!a[i, j].Equals(b[i, j]))
                        return false;
                }
            }
            return true;
        }

        /// <summary>Compares two vectors for equality.</summary>
        public static bool IsEqual<T>(this T[] a, T[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (!a[i].Equals(b[i]))
                    return false;
            }
            return true;
        }
        
        #endregion
        #region algebra

        /// <summary>
        ///   Multiplies a matrix by itself n times.
        /// </summary>
        public static double[,] Power(double[,] m, int n)
        {
            if (!m.IsSquare())
                throw new ArgumentException("Matrix must be square");

            // TODO: this is a very naive implementation. Optimize.
            double[,] r = m;
            for (int i = 0; i < n; i++)
                r = r.Multiply(m);

            return r;
        }

        /// <summary>
        ///   Gets the transpose of a matrix.
        /// </summary>
        /// <param name="m">A matrix.</param>
        /// <returns>The transpose of matrix m.</returns>
        public static T[,] Transpose<T>(this T[,] m)
        {
            T[,] t = new T[m.GetLength(1), m.GetLength(0)];
            for (int i = 0; i < m.GetLength(0); i++)
                for (int j = 0; j < m.GetLength(1); j++)
                    t[j, i] = m[i, j];

            return t;
        }

        /// <summary>
        ///   Gets the transpose of a vector.
        /// </summary>
        /// <param name="m">A vector.</param>
        /// <returns>The transpose of vector m.</returns>
        public static T[,] Transpose<T>(this T[] m)
        {
            T[,] t = new T[1, m.GetLength(0)];
            for (int i = 0; i < m.Length; i++)
            {
                t[0, i] = m[i];
            }

            return t;
        }

        #region multiplication

        /// <summary>
        ///   Multiplies two matrices.
        /// </summary>
        /// <param name="a">The left matrix.</param>
        /// <param name="b">The right matrix.</param>
        /// <returns>The product of the multiplication of the two matrices.</returns>
        public static double[,] Multiply(this double[,] a, double[,] b)
        {
            int m = a.GetLength(0);
            int n = b.GetLength(1);
            int p = a.GetLength(1);

            double[,] r = new double[m, n];

            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    for (int k = 0; k < p; k++)
                        r[i, j] += a[i, k] * b[k, j];

            return r;
        }

        /// <summary>
        ///   Multiplies two matrices.
        /// </summary>
        /// <param name="a">The left matrix.</param>
        /// <param name="b">The right matrix.</param>
        /// <returns>The product of the multiplication of the two matrices.</returns>
        public static float[,] Multiply(this float[,] a, float[,] b)
        {
            int m = a.GetLength(0);
            int n = b.GetLength(1);
            int p = a.GetLength(1);

            float[,] r = new float[m, n];

            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    for (int k = 0; k < p; k++)
                        r[i, j] += a[i, k] * b[k, j];

            return r;
        }

        /// <summary>
        ///   Multiplies a vector and a matrix.
        /// </summary>
        /// <param name="a">A row vector.</param>
        /// <param name="b">A matrix.</param>
        /// <returns>The product of the multiplication of the row vector and the matrix.</returns>
        public static double[] Multiply(this double[] a, double[,] b)
        {
            if (b.GetLength(0) != a.Length)
                throw new Exception("Matrix dimensions must match");

            double[] r = new double[b.GetLength(1)];

            for (int j = 0; j < b.GetLength(1); j++)
                for (int k = 0; k < a.Length; k++)
                    r[j] += a[k] * b[k, j];

            return r;
        }

        /// <summary>
        ///   Multiplies a matrix and a vector (a*bT).
        /// </summary>
        /// <param name="a">A matrix.</param>
        /// <param name="b">A column vector.</param>
        /// <returns>The product of the multiplication of matrix a and column vector b.</returns>
        public static double[] Multiply(this double[,] a, double[] b)
        {
            if (a.GetLength(1) != b.Length)
                throw new Exception("Matrix dimensions must match");

            double[] r = new double[a.GetLength(0)];

            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < b.Length; j++)
                    r[i] += a[i, j] * b[j];

            return r;
        }

        /// <summary>
        ///   Multiplies a matrix by a scalar.
        /// </summary>
        /// <param name="a">A matrix.</param>
        /// <param name="x">A scalar.</param>
        /// <returns>The product of the multiplication of matrix a and scalar x.</returns>
        public static double[,] Multiply(this double[,] a, double x)
        {
            double[,] r = new double[a.GetLength(0), a.GetLength(1)];

            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < a.GetLength(1); j++)
                    r[i, j] = a[i, j] * x;

            return r;
        }

        /// <summary>
        ///   Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="a">A vector.</param>
        /// <param name="x">A scalar.</param>
        /// <returns>The product of the multiplication of vector a and scalar x.</returns>
        public static double[] Multiply(this double[] a, double x)
        {
            double[] r = new double[a.Length];

            for (int i = 0; i < a.GetLength(0); i++)
                r[i] = a[i] * x;

            return r;
        }

        /// <summary>
        ///   Multiplies a matrix by a scalar.
        /// </summary>
        /// <param name="a">A scalar.</param>
        /// <param name="x">A matrix.</param>
        /// <returns>The product of the multiplication of vector a and scalar x.</returns>
        public static double[,] Multiply(this double x, double[,] a)
        {
            return a.Multiply(x);
        }

        /// <summary>
        ///   Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="a">A scalar.</param>
        /// <param name="x">A matrix.</param>
        /// <returns>The product of the multiplication of vector a and scalar x.</returns>
        public static double[] Multiply(this double x, double[] a)
        {
            return a.Multiply(x);
        }

        #endregion
        #region division

        /// <summary>
        ///   Divides a vector by a scalar.
        /// </summary>
        /// <param name="a">A vector.</param>
        /// <param name="x">A scalar.</param>
        /// <returns>The division quocient of vector a and scalar x.</returns>
        public static double[] Divide(this double[] a, double x)
        {
            double[] r = new double[a.Length];

            for (int i = 0; i < a.GetLength(0); i++)
                r[i] = a[i] / x;

            return r;
        }

        /// <summary>
        ///   Divides two matrices by multiplying A by the inverse of B.
        /// </summary>
        /// <param name="a">The first matrix.</param>
        /// <param name="b">The second matrix (which will be inversed).</param>
        /// <returns>The result from the division of a and b.</returns>
        public static double[,] Divide(this double[,] a, double[,] b)
        {
            return a.Multiply(b.Inverse());
        }

        /// <summary>
        ///   Divides a matrix by a scalar.
        /// </summary>
        /// <param name="a">A matrix.</param>
        /// <param name="x">A scalar.</param>
        /// <returns>The division quocient of matrix a and scalar x.</returns>
        public static double[,] Divide(this double[,] a, double x)
        {
            double[,] r = new double[a.GetLength(0), a.GetLength(1)];

            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < a.GetLength(1); j++)
                    r[i, j] = a[i, j] / x;

            return r;
        }

        #endregion
        #region products

        /// <summary>
        ///   Gets the inner product (scalar product) between two vectors (aT*b).
        /// </summary>
        /// <param name="a">A vector.</param>
        /// <param name="b">A vector.</param>
        /// <returns>The inner product of the multiplication of the vectors.</returns>
        /// <remarks>
        ///    In mathematics, the dot product is an algebraic operation that takes two
        ///    equal-length sequences of numbers (usually coordinate vectors) and returns
        ///    a single number obtained by multiplying corresponding entries and adding up
        ///    those products. The name is derived from the dot that is often used to designate
        ///    this operation; the alternative name scalar product emphasizes the scalar
        ///    (rather than vector) nature of the result.
        ///    
        ///    The principal use of this product is the inner product in a Euclidean vector space:
        ///    when two vectors are expressed on an orthonormal basis, the dot product of their 
        ///    coordinate vectors gives their inner product.
        /// </remarks>
        public static double InnerProduct(this double[] a, double[] b)
        {
            double r = 0.0;

            if (a.Length != b.Length)
                throw new Exception("Vector dimensions must match");

            for (int i = 0; i < a.Length; i++)
                r += a[i] * b[i];

            return r;
        }

        /// <summary>
        ///   Gets the outer product (matrix product) between two vectors (a*bT).
        /// </summary>
        /// <remarks>
        ///   In linear algebra, the outer product typically refers to the tensor
        ///   product of two vectors. The result of applying the outer product to
        ///   a pair of vectors is a matrix. The name contrasts with the inner product,
        ///   which takes as input a pair of vectors and produces a scalar.
        /// </remarks>
        public static double[,] OuterProduct(this double[] a, double[] b)
        {
            double[,] r = new double[a.Length, b.Length];

            for (int i = 0; i < a.Length; i++)
                for (int j = 0; j < b.Length; j++)
                    r[i, j] += a[i] * b[j];

            return r;
        }

        /// <summary>
        ///   Vectorial product.
        /// </summary>
        /// <remarks>
        ///   The cross product, vector product or Gibbs vector product is a binary operation
        ///   on two vectors in three-dimensional space. It has a vector result, a vector which
        ///   is always perpendicular to both of the vectors being multiplied and the plane
        ///   containing them. It has many applications in mathematics, engineering and physics.
        /// </remarks>
        public static double[] VectorProduct(double[] a, double[] b)
        {
            return new double[] {
                a[1]*b[2] - a[2]*b[1],
                a[2]*b[0] - a[0]*b[2],
                a[0]*b[1] - a[1]*b[0]
            };
        }

        /// <summary>
        ///   Vectorial product.
        /// </summary>
        public static float[] VectorProduct(float[] a, float[] b)
        {
            return new float[] {
                a[1]*b[2] - a[2]*b[1],
                a[2]*b[0] - a[0]*b[2],
                a[0]*b[1] - a[1]*b[0]
            };
        }

        #endregion
        #region add and subtract

        /// <summary>
        ///   Adds two matrices.
        /// </summary>
        /// <param name="a">A matrix.</param>
        /// <param name="b">A matrix.</param>
        /// <returns>The sum of the two matrices a and b.</returns>
        public static double[,] Add(this double[,] a, double[,] b)
        {
            if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
                throw new Exception("Matrix dimensions must match");

            double[,] r = new double[a.GetLength(0), a.GetLength(1)];

            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < a.GetLength(1); j++)
                    r[i, j] = a[i, j] + b[i, j];

            return r;
        }

        /// <summary>
        ///   Adds a vector to a column or row of a matrix.
        /// </summary>
        /// <param name="a">A matrix.</param>
        /// <param name="v">A vector.</param>
        /// <param name="dimension">
        ///   Pass 0 if the vector should be added row-wise, 
        ///   or 1 if the vector should be added column-wise.
        /// </param>
        public static double[,] Add(this double[,] a, double[] v, int dimension)
        {
            double[,] r = new double[a.GetLength(0), a.GetLength(1)];

            if (dimension == 1)
            {
                for (int i = 0; i < a.GetLength(0); i++)
                    for (int j = 0; j < a.GetLength(1); j++)
                        r[i, j] = a[i, j] + v[i];
            }
            else
            {
                for (int i = 0; i < a.GetLength(0); i++)
                    for (int j = 0; j < a.GetLength(1); j++)
                        r[i, j] = a[i, j] + v[j];
            }

            return r;
        }

        /// <summary>
        ///   Adds a vector to a column or row of a matrix.
        /// </summary>
        /// <param name="a">A matrix.</param>
        /// <param name="v">A vector.</param>
        /// <param name="dimension">The dimension to add the vector to.</param>
        public static double[,] Subtract(this double[,] a, double[] v, int dimension)
        {
            double[,] r = new double[a.GetLength(0), a.GetLength(1)];

            if (dimension == 0)
            {
                for (int i = 0; i < a.GetLength(0); i++)
                    for (int j = 0; j < a.GetLength(1); j++)
                        r[i, j] = a[i, j] - v[i];
            }
            else
            {
                for (int i = 0; i < a.GetLength(0); i++)
                    for (int j = 0; j < a.GetLength(1); j++)
                        r[i, j] = a[i, j] - v[j];
            }

            return r;
        }

        /// <summary>
        ///   Adds two vectors.
        /// </summary>
        /// <param name="a">A vector.</param>
        /// <param name="b">A vector.</param>
        /// <returns>The addition of vector a to vector b.</returns>
        public static double[] Add(this double[] a, double[] b)
        {
            double[] r = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
                r[i] = a[i] + b[i];

            return r;
        }

        /// <summary>
        ///   Subtracts two matrices.
        /// </summary>
        /// <param name="a">A matrix.</param>
        /// <param name="b">A matrix.</param>
        /// <returns>The subtraction of matrix b from matrix a.</returns>
        public static double[,] Subtract(this double[,] a, double[,] b)
        {
            double[,] r = new double[a.GetLength(0), a.GetLength(1)];

            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < a.GetLength(1); j++)
                    r[i, j] = a[i, j] - b[i, j];

            return r;
        }

        /// <summary>
        ///   Subtracts two vectors.
        /// </summary>
        /// <param name="a">A vector.</param>
        /// <param name="b">A vector.</param>
        /// <returns>The subtraction of vector b from vector a.</returns>
        public static double[] Subtract(this double[] a, double[] b)
        {
            double[] r = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
                r[i] = a[i] - b[i];

            return r;
        }

        /// <summary>
        ///   Subtracts a scalar from a vector.
        /// </summary>
        /// <param name="a">A vector.</param>
        /// <param name="b">A scalar.</param>
        /// <returns>The subtraction of b from all elements in a.</returns>
        public static double[] Subtract(this double[] a, double b)
        {
            double[] r = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
                r[i] = a[i] - b;

            return r;
        }

        #endregion

        #endregion
        #region factories

        /// <summary>
        /// Gets the diagonal vector from a matrix.
        /// </summary>
        /// <param name="m">A matrix.</param>
        /// <returns>The diagonal vector from matrix m.</returns>
        public static double[] Diagonal(this double[,] m)
        {
            double[] r = new double[m.GetLength(0)];

            for (int i = 0; i < r.Length; i++)
                r[i] = m[i, i];

            return r;
        }

        /// <summary>
        ///   Returns a square diagonal matrix of the given size.
        /// </summary>
        public static double[,] Diagonal(int size, double value)
        {
            double[,] m = new double[size, size];

            for (int i = 0; i < size; i++)
                m[i, i] = value;

            return m;
        }

        /// <summary>
        ///   Returns a matrix of the given size with value on its diagonal.
        /// </summary>
        public static double[,] Diagonal(int rows, int cols, double value)
        {
            double[,] m = new double[rows, cols];

            for (int i = 0; i < rows; i++)
                m[i, i] = value;

            return m;
        }

        /// <summary>
        ///   Return a square matrix with a vector of values on its diagonal.
        /// </summary>
        public static double[,] Diagonal(double[] values)
        {
            double[,] m = new double[values.Length, values.Length];

            for (int i = 0; i < values.Length; i++)
                m[i, i] = values[i];

            return m;
        }

        /// <summary>
        ///   Return a square matrix with a vector of values on its diagonal.
        /// </summary>
        public static T[,] Diagonal<T>(int size, T[] values)
        {
            return Diagonal(size, size, values);
        }

        /// <summary>
        ///   Returns a matrix with a vector of values on its diagonal.
        /// </summary>
        public static T[,] Diagonal<T>(int rows, int cols, T[] values)
        {
            T[,] m = new T[rows, cols];

            for (int i = 0; i < values.Length; i++)
                m[i, i] = values[i];

            return m;
        }

        /// <summary>
        ///   Returns a matrix with all elements set to a given value.
        /// </summary>
        public static T[,] Create<T>(int rows, int cols, T value)
        {
            T[,] m = new T[rows, cols];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    m[i, j] = value;

            return m;
        }

        /// <summary>
        ///   Returns a matrix with all elements set to a given value.
        /// </summary>
        public static T[,] Create<T>(int size, T value)
        {
            return Create(size, size, value);
        }

        /// <summary>
        ///   Expands a data vector given in summary form.
        /// </summary>
        /// <param name="data">A base vector.</param>
        /// <param name="count">An array containing by how much each line should be replicated.</param>
        /// <returns></returns>
        public static T[] Expand<T>(T[] data, int[] count)
        {
            var expansion = new List<T>();
            for (int i = 0; i < count.Length; i++)
                for (int j = 0; j < count[i]; j++)
                    expansion.Add(data[i]);

            return expansion.ToArray();
        }

        /// <summary>
        ///   Expands a data matrix given in summary form.
        /// </summary>
        /// <param name="data">A base matrix.</param>
        /// <param name="count">An array containing by how much each line should be replicated.</param>
        /// <returns></returns>
        public static T[][] Expand<T>(T[][] data, int[] count)
        {
            var expansion = new List<T[]>();
            for (int i = 0; i < count.Length; i++)
                for (int j = 0; j < count[i]; j++)
                    expansion.Add(data[i]);

            return expansion.ToArray();
        }

        /// <summary>
        ///   Expands a data matrix given in summary form.
        /// </summary>
        /// <param name="data">A base matrix.</param>
        /// <param name="count">An array containing by how much each line should be replicated.</param>
        /// <returns></returns>
        public static T[,] Expand<T>(T[,] data, int[] count)
        {
            var expansion = new List<T[]>();
            for (int i = 0; i < count.Length; i++)
                for (int j = 0; j < count[i]; j++)
                    expansion.Add(data.GetRow(i));

            return expansion.ToArray().ToMatrix();
        }

        /// <summary>
        ///   Returns the Identity matrix of the given size.
        /// </summary>
        public static double[,] Identity(int size)
        {
            return Diagonal(size, 1.0);
        }

        /// <summary>
        ///   Creates a centering matrix of size n x n in the form (I - 1n)
        ///   where 1n is a matrix with all entries 1/n.
        /// </summary>
        public static double[,] Centering(int size)
        {
            double[,] r = MMath.Create(size, -1.0 / size);
            for (int i = 0; i < size; i++)
                r[i, i] = 1.0 - 1.0 / size;

            return r;
        }

        /// <summary>
        ///   Creates a matrix with a single row vector.
        /// </summary>
        public static T[,] RowVector<T>(params T[] values)
        {
            T[,] r = new T[1, values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                r[0, i] = values[i];
            }

            return r;
        }

        /// <summary>
        ///   Creates a matrix with a single column vector.
        /// </summary>
        public static T[,] ColumnVector<T>(T[] values)
        {
            T[,] r = new T[values.Length, 1];

            for (int i = 0; i < values.Length; i++)
            {
                r[i, 0] = values[i];
            }

            return r;
        }

        /// <summary>
        ///   Creates an index vector.
        /// </summary>
        public static int[] Indices(int from, int to)
        {
            int[] r = new int[to - from];
            for (int i = 0; i < r.Length; i++)
                r[i] = from++;
            return r;
        }

        /// <summary>
        ///   Combines two vectors horizontally.
        /// </summary>
        public static T[] Combine<T>(this T[] a1, T[] a2)
        {
            T[] r = new T[a1.Length + a2.Length];
            for (int i = 0; i < a1.Length; i++)
                r[i] = a1[i];
            for (int i = 0; i < a2.Length; i++)
                r[i + a1.Length] = a2[i];

            return r;
        }

        /// <summary>
        ///   Combines a vector and a element horizontally.
        /// </summary>
        public static T[] Combine<T>(this T[] a1, T a2)
        {
            T[] r = new T[a1.Length + 1];
            for (int i = 0; i < a1.Length; i++)
                r[i] = a1[i];

            r[a1.Length] = a2;

            return r;
        }

        /// <summary>
        ///   Combine vectors horizontally.
        /// </summary>
        public static T[] Combine<T>(params T[][] vectors)
        {
            int size = 0;
            for (int i = 0; i < vectors.Length; i++)
                size += vectors[i].Length;

            T[] r = new T[size];

            int c = 0;
            for (int i = 0; i < vectors.Length; i++)
                for (int j = 0; j < vectors[i].Length; j++)
                    r[c++] = vectors[i][j];

            return r;
        }

        /// <summary>
        ///   Combines matrices vertically.
        /// </summary>
        public static T[,] Combine<T>(params T[][,] matrices)
        {
            int rows = 0;
            int cols = 0;

            for (int i = 0; i < matrices.Length; i++)
            {
                rows += matrices[i].GetLength(0);
                if (matrices[i].GetLength(1) > cols)
                    cols = matrices[i].GetLength(1);
            }

            T[,] r = new T[rows, cols];

            int c = 0;
            for (int i = 0; i < matrices.Length; i++)
            {
                for (int j = 0; j < matrices[i].GetLength(0); j++)
                {
                    for (int k = 0; k < matrices[i].GetLength(1); k++)
                        r[c, k] = matrices[i][j, k];
                    c++;
                }
            }

            return r;
        }

        #endregion
        #region subsection

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="data">The matrix to return the submatrix from.</param>
        /// <param name="startRow">Start row index</param>
        /// <param name="endRow">End row index</param>
        /// <param name="startColumn">Start column index</param>
        /// <param name="endColumn">End column index</param>
        /// <remarks>
        ///   Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
        /// </remarks>
        public static T[,] Submatrix<T>(this T[,] data, int startRow, int endRow, int startColumn, int endColumn)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            if ((startRow > endRow) || (startColumn > endColumn) || (startRow < 0) ||
                (startRow >= rows) || (endRow < 0) || (endRow >= rows) ||
                (startColumn < 0) || (startColumn >= cols) || (endColumn < 0) ||
                (endColumn >= cols))
            {
                throw new ArgumentException("Argument out of range.");
            }

            T[,] X = new T[endRow - startRow + 1, endColumn - startColumn + 1];
            for (int i = startRow; i <= endRow; i++)
            {
                for (int j = startColumn; j <= endColumn; j++)
                {
                    X[i - startRow, j - startColumn] = data[i, j];
                }
            }

            return X;
        }

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="data">The matrix to return the submatrix from.</param>
        /// <param name="rowIndexes">Array of row indices</param>
        /// <param name="columnIndexes">Array of column indices</param>
        /// <remarks>
        ///   Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
        /// </remarks>
        public static T[,] Submatrix<T>(this T[,] data, int[] rowIndexes, int[] columnIndexes)
        {
            T[,] X = new T[rowIndexes.Length, columnIndexes.Length];

            for (int i = 0; i < rowIndexes.Length; i++)
            {
                for (int j = 0; j < columnIndexes.Length; j++)
                {
                    if ((rowIndexes[i] < 0) || (rowIndexes[i] >= data.GetLength(0)) ||
                        (columnIndexes[j] < 0) || (columnIndexes[j] >= data.GetLength(1)))
                    {
                        throw new ArgumentException("Argument out of range.");
                    }

                    X[i, j] = data[rowIndexes[i], columnIndexes[j]];
                }
            }

            return X;
        }

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="data">The matrix to return the submatrix from.</param>
        /// <param name="rowIndexes">Array of row indices</param>
        /// <remarks>
        ///   Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
        /// </remarks>
        public static T[,] Submatrix<T>(this T[,] data, int[] rowIndexes)
        {
            T[,] X = new T[rowIndexes.Length, data.GetLength(1)];

            for (int i = 0; i < rowIndexes.Length; i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    if ((rowIndexes[i] < 0) || (rowIndexes[i] >= data.GetLength(0)))
                    {
                        throw new ArgumentException("Argument out of range.");
                    }

                    X[i, j] = data[rowIndexes[i], j];
                }
            }

            return X;
        }

        /// <summary>Returns a subvector extracted from the current vector.</summary>
        /// <param name="data">The vector to return the subvector from.</param>
        /// <param name="indexes">Array of indices.</param>
        /// <remarks>
        ///   Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
        /// </remarks>
        public static T[] Submatrix<T>(this T[] data, int[] indexes)
        {
            T[] X = new T[indexes.Length];

            for (int i = 0; i < indexes.Length; i++)
            {
                X[i] = data[indexes[i]];
            }

            return X;
        }

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="data">The vector to return the subvector from.</param>
        /// <param name="i0">Starting index.</param>
        /// <param name="i1">End index.</param>
        /// <remarks>
        ///   Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
        /// </remarks>
        public static T[] Submatrix<T>(this T[] data, int i0, int i1)
        {
            T[] X = new T[i1 - i0 + 1];

            for (int i = i0; i <= i1; i++)
            {
                X[i] = data[i];
            }

            return X;
        }

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <remarks>
        ///   Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
        /// </remarks>
        public static T[] Submatrix<T>(this T[] data, int first)
        {
            return Submatrix<T>(data, 0, first - 1);
        }

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="data">The matrix to return the submatrix from.</param>
        /// <param name="i0">Starting row index</param>
        /// <param name="i1">End row index</param>
        /// <param name="c">Array of column indices</param>
        /// <remarks>
        ///   Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
        /// </remarks>
        public static T[,] Submatrix<T>(this T[,] data, int i0, int i1, int[] c)
        {
            if ((i0 > i1) || (i0 < 0) || (i0 >= data.GetLength(0))
                || (i1 < 0) || (i1 >= data.GetLength(0)))
            {
                throw new ArgumentException("Argument out of range.");
            }

            T[,] X = new T[i1 - i0 + 1, c.Length];

            for (int i = i0; i <= i1; i++)
            {
                for (int j = 0; j < c.Length; j++)
                {
                    if ((c[j] < 0) || (c[j] >= data.GetLength(1)))
                    {
                        throw new ArgumentException("Argument out of range.");
                    }

                    X[i - i0, j] = data[i, c[j]];
                }
            }

            return X;
        }

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="data">The matrix to return the submatrix from.</param>
        /// <param name="r">Array of row indices</param>
        /// <param name="j0">Start column index</param>
        /// <param name="j1">End column index</param>
        /// <remarks>
        ///   Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
        /// </remarks>
        public static T[,] Submatrix<T>(this T[,] data, int[] r, int j0, int j1)
        {
            if ((j0 > j1) || (j0 < 0) || (j0 >= data.GetLength(1)) || (j1 < 0)
                || (j1 >= data.GetLength(1)))
            {
                throw new ArgumentException("Argument out of range.");
            }

            T[,] X = new T[r.Length, j1 - j0 + 1];

            for (int i = 0; i < r.Length; i++)
            {
                for (int j = j0; j <= j1; j++)
                {
                    if ((r[i] < 0) || (r[i] >= data.GetLength(0)))
                    {
                        throw new ArgumentException("Argument out of range.");
                    }

                    X[i, j - j0] = data[r[i], j];
                }
            }

            return X;
        }

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="data">The matrix to return the submatrix from.</param>
        /// <param name="i0">Starting row index</param>
        /// <param name="i1">End row index</param>
        /// <param name="c">Array of column indices</param>
        /// <remarks>
        ///   Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
        /// </remarks>
        public static T[][] Submatrix<T>(this T[][] data, int i0, int i1, int[] c)
        {
            if ((i0 > i1) || (i0 < 0) || (i0 >= data.Length)
                || (i1 < 0) || (i1 >= data.Length))
            {
                throw new ArgumentException("Argument out of range");
            }

            T[][] X = new T[i1 - i0 + 1][];

            for (int i = i0; i <= i1; i++)
            {
                X[i] = new T[c.Length];

                for (int j = 0; j < c.Length; j++)
                {
                    if ((c[j] < 0) || (c[j] >= data[i].Length))
                    {
                        throw new ArgumentException("Argument out of range.");
                    }

                    X[i - i0][j] = data[i][c[j]];
                }
            }

            return X;
        }

        #endregion
        #region manipulate

        /// <summary>
        ///   Returns a new matrix without one of its columns.
        /// </summary>
        public static T[][] RemoveColumn<T>(this T[][] m, int index)
        {
            T[][] X = new T[m.Length][];

            for (int i = 0; i < m.Length; i++)
            {
                X[i] = new T[m[i].Length - 1];
                for (int j = 0; j < index; j++)
                {
                    X[i][j] = m[i][j];
                }
                for (int j = index + 1; j < m[i].Length; j++)
                {
                    X[i][j - 1] = m[i][j];
                }
            }
            return X;
        }

        /// <summary>
        ///   Returns a new matrix with a given column vector inserted at a given index.
        /// </summary>
        public static T[,] InsertColumn<T>(this T[,] m, T[] column, int index)
        {
            int rows = m.GetLength(0);
            int cols = m.GetLength(1);

            T[,] X = new T[rows, cols + 1];

            for (int i = 0; i < rows; i++)
            {
                // Copy original matrix
                for (int j = 0; j < index; j++)
                {
                    X[i, j] = m[i, j];
                }
                for (int j = index; j < cols; j++)
                {
                    X[i, j + 1] = m[i, j];
                }

                // Copy additional column
                X[i, index] = column[i];
            }

            return X;
        }

        /// <summary>
        ///   Removes an element from a vector.
        /// </summary>
        public static T[] RemoveAt<T>(this T[] array, int index)
        {
            T[] r = new T[array.Length - 1];
            for (int i = 0; i < index; i++)
                r[i] = array[i];
            for (int i = index; i < r.Length; i++)
                r[i] = array[i + 1];

            return r;
        }

        /// <summary>
        ///   Stores a column vector into the given column position of the matrix.
        /// </summary>
        public static T[,] SetColumn<T>(this T[,] m, int index, T[] column)
        {
            for (int i = 0; i < column.Length; i++)
                m[i, index] = column[i];

            return m;
        }

        /// <summary>
        ///   Stores a row vector into the given row position of the matrix.
        /// </summary>
        public static T[,] SetRow<T>(this T[,] m, int index, T[] row)
        {
            for (int i = 0; i < row.Length; i++)
                m[index, i] = row[i];

            return m;
        }

        /// <summary>
        ///   Applies a function to every element of the array.
        /// </summary>
        public static TResult[] Apply<TData, TResult>(this TData[] data, Func<TData, TResult> func)
        {
            var r = new TResult[data.Length];
            for (int i = 0; i < data.Length; i++)
                r[i] = func(data[i]);
            return r;
        }

        /// <summary>
        ///   Sorts the columns of a matrix by sorting keys.
        /// </summary>
        /// <param name="keys">The key value for each column.</param>
        /// <param name="values">The matrix to be sorted.</param>
        /// <param name="comparer">The comparer to use.</param>
        public static TValue[,] Sort<TKey, TValue>(TKey[] keys, TValue[,] values, IComparer<TKey> comparer)
        {
            int[] indices = new int[keys.Length];
            for (int i = 0; i < keys.Length; i++) indices[i] = i;

            Array.Sort<TKey, int>(keys, indices, comparer);

            return values.Submatrix(0, values.GetLength(0) - 1, indices);
        }

        #endregion
        #region search

        /// <summary>
        ///   Gets a column vector from a matrix.
        /// </summary>
        public static T[] GetColumn<T>(this T[,] m, int index)
        {
            T[] column = new T[m.GetLength(0)];

            for (int i = 0; i < column.Length; i++)
                column[i] = m[i, index];

            return column;
        }

        /// <summary>
        ///   Gets a column vector from a matrix.
        /// </summary>
        public static T[] GetColumn<T>(this T[][] m, int index)
        {
            T[] column = new T[m.Length];

            for (int i = 0; i < column.Length; i++)
                column[i] = m[i][index];

            return column;
        }

        /// <summary>
        ///   Gets a row vector from a matrix.
        /// </summary>
        public static T[] GetRow<T>(this T[,] m, int index)
        {
            T[] row = new T[m.GetLength(1)];

            for (int i = 0; i < row.Length; i++)
                row[i] = m[index, i];

            return row;
        }

        /// <summary>
        ///   Gets the indices of all elements matching a certain criteria.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="data">The array to search inside.</param>
        /// <param name="func">The search criteria.</param>
        public static int[] Find<T>(this T[] data, Func<T, bool> func)
        {
            return Find(data, func, false);
        }

        /// <summary>
        ///   Gets the indices of all elements matching a certain criteria.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="data">The array to search inside.</param>
        /// <param name="func">The search criteria.</param>
        /// <param name="firstOnly">
        ///    Set to true to stop when the first element is
        ///    found, set to false to get all elements.
        /// </param>
        public static int[] Find<T>(this T[] data, Func<T, bool> func, bool firstOnly)
        {
            List<int> idx = new List<int>();
            for (int i = 0; i < data.Length; i++)
            {
                if (func(data[i]))
                {
                    if (firstOnly)
                        return new int[] { i };
                    else idx.Add(i);
                }
            }
            return idx.ToArray();
        }

        /// <summary>
        ///   Gets the indices of all elements matching a certain criteria.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="data">The array to search inside.</param>
        /// <param name="func">The search criteria.</param>
        public static int[][] Find<T>(this T[,] data, Func<T, bool> func)
        {
            return Find(data, func, false);
        }

        /// <summary>
        ///   Gets the indices of all elements matching a certain criteria.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="data">The array to search inside.</param>
        /// <param name="func">The search criteria.</param>
        /// <param name="firstOnly">
        ///    Set to true to stop when the first element is
        ///    found, set to false to get all elements.
        /// </param>
        public static int[][] Find<T>(this T[,] data, Func<T, bool> func, bool firstOnly)
        {
            List<int[]> idx = new List<int[]>();
            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    if (func(data[i, j]))
                    {
                        if (firstOnly)
                            return new int[][] { new int[] { i, j } };
                        else idx.Add(new int[] { i, j });
                    }
                }
            }
            return idx.ToArray();
        }
        
        #endregion
        #region properties

        /// <summary>
        ///   Returns true if a matrix is square.
        /// </summary>
        public static bool IsSquare<T>(this T[,] matrix)
        {
            return matrix.GetLength(0) == matrix.GetLength(1);
        }

        /// <summary>
        ///   Returns true if a matrix is symmetric.
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static bool IsSymmetric(this double[,] matrix)
        {
            if (matrix.GetLength(0) == matrix.GetLength(1))
            {
                for (int i = 0; i < matrix.GetLength(0); i++)
                {
                    for (int j = 0; j <= i; j++)
                    {
                        if (matrix[i, j] != matrix[j, i])
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        ///   Gets the trace of a matrix.
        /// </summary>
        /// <remarks>
        ///   The trace of an n-by-n square matrix A is defined to be the sum of the
        ///   elements on the main diagonal (the diagonal from the upper left to the
        ///   lower right) of A.
        /// </remarks>
        public static double Trace(this double[,] m)
        {
            double trace = 0.0;
            for (int i = 0; i < m.GetLength(0); i++)
            {
                trace += m[i, i];
            }
            return trace;
        }

        /// <summary>
        ///   Gets the Squared Euclidean norm for a vector.
        /// </summary>
        public static double SquareNorm(this double[] a)
        {
            double sum = 0.0;
            for (int i = 0; i < a.Length; i++)
                sum += a[i] * a[i];
            return sum;
        }

        /// <summary>
        ///   Gets the Euclidean norm for a vector.
        /// </summary>
        public static double Norm(this double[] a)
        {
            return System.Math.Sqrt(SquareNorm(a));
        }

        /// <summary>
        ///   Gets the Squared Euclidean norm vector for a matrix.
        /// </summary>
        public static double[] SquareNorm(this double[,] a)
        {
            double[] norm = new double[a.GetLength(1)];
            double sum;
            for (int j = 0; j < norm.Length; j++)
            {
                sum = 0.0;
                for (int i = 0; i < a.GetLength(0); i++)
                    sum += a[i, j] * a[i, j];
                norm[j] = sum;
            }
            return norm;
        }

        /// <summary>
        ///   Gets the Euclidean norm for a vector.
        /// </summary>
        public static double[] Norm(this double[,] a)
        {
            double[] norm = MMath.SquareNorm(a);
            return MMath.Sqrt(norm);
        }

        /// <summary>Calculates the matrix Sum vector.</summary>
        /// <param name="value">A matrix whose sums will be calculated.</param>
        /// <returns>Returns a vector containing the sums of each variable in the given matrix.</returns>
        public static double[] Sum(double[,] value)
        {
            double[] sum = new double[value.GetLength(1)];

            // for each row
            for (int i = 0; i < value.GetLength(0); i++)
            {
                // for each column
                for (int j = 0; j < value.GetLength(1); j++)
                {
                    sum[j] += value[i, j];
                }
            }
            return sum;
        }

        /// <summary>Calculates the matrix Sum vector.</summary>
        /// <param name="value">A matrix whose sums will be calculated.</param>
        /// <returns>Returns a vector containing the sums of each variable in the given matrix.</returns>
        public static int[] Sum(int[,] value)
        {
            int[] sum = new int[value.GetLength(1)];

            // for each row
            for (int i = 0; i < value.GetLength(0); i++)
            {
                // for each column
                for (int j = 0; j < value.GetLength(1); j++)
                {
                    sum[j] += value[i, j];
                }
            }
            return sum;
        }

        /// <summary>
        ///   Gets the sum of all elements in a vector.
        /// </summary>
        public static double Sum(this double[] value)
        {
            double sum = 0.0;

            for (int i = 0; i < value.Length; i++)
                sum += value[i];

            return sum;
        }

        /// <summary>
        ///   Gets the product of all elements in a vector.
        /// </summary>
        public static double Product(this double[] value)
        {
            double product = 0.0;

            for (int i = 0; i < value.Length; i++)
                product *= value[i];

            return product;
        }

        /// <summary>
        ///   Gets the sum of all elements in a vector.
        /// </summary>
        public static int Sum(this int[] vector)
        {
            int r = 0;
            for (int i = 0; i < vector.Length; i++)
                r += vector[i];
            return r;
        }

        /// <summary>
        ///   Gets the maximum element in a vector.
        /// </summary>
        public static double Max(this double[] vector)
        {
            double max = vector[0];
            for (int i = 0; i < vector.Length; i++)
            {
                if (vector[i] > max)
                    max = vector[i];
            }
            return max;
        }

        /// <summary>
        ///   Gets the maximum element in a vector.
        /// </summary>
        public static double Max(this double[] x, out int imax)
        {
            imax = 0;
            double max = x[0];
            for (int i = 1; i < x.Length; i++)
            {
                if (x[i] > max)
                {
                    max = x[i];
                    imax = i;
                }
            }
            return max;
        }

        /// <summary>
        ///   Gets the minimum element in a vector.
        /// </summary>
        public static double Min(this double[] x, out int imin)
        {
            imin = 0;
            double min = x[0];
            for (int i = 1; i < x.Length; i++)
            {
                if (x[i] < min)
                {
                    min = x[i];
                    imin = i;
                }
            }
            return min;
        }

        /// <summary>
        ///   Gets the minimum element in a vector.
        /// </summary>
        public static double Min(this double[] x)
        {
            double min = x[0];
            for (int i = 1; i < x.Length; i++)
            {
                if (x[i] < min)
                    min = x[i];
            }
            return min;
        }

        /// <summary>
        ///   Gets the maximum values accross one dimension of a matrix.
        /// </summary>
        public static double[] Max(double[,] matrix, int dimension, out int[] imax)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double[] max;

            if (dimension == 1) // Search down columns
            {
                imax = new int[rows];
                max = matrix.GetColumn(0);

                for (int j = 0; j < rows; j++)
                {
                    for (int i = 1; i < cols; i++)
                    {
                        if (matrix[j, i] > max[j])
                        {
                            max[j] = matrix[j, i];
                            imax[j] = i;
                        }
                    }
                }
            }
            else
            {
                imax = new int[cols];
                max = matrix.GetRow(0);

                for (int j = 0; j < cols; j++)
                {
                    for (int i = 1; i < rows; i++)
                    {
                        if (matrix[i, j] > max[j])
                        {
                            max[j] = matrix[i, j];
                            imax[j] = i;
                        }
                    }
                }
            }

            return max;
        }

        /// <summary>
        ///   Gets the range of the values in a vector.
        /// </summary>
        public static RangeDouble Range(this double[] array)
        {
            double min = array[0];
            double max = array[0];

            for (int i = 1; i < array.Length; i++)
            {
                if (min > array[i])
                    min = array[i];
                if (max < array[i])
                    max = array[i];
            }
            return new RangeDouble(min, max);
        }

        /// <summary>
        ///   Gets the range of the values in a vector.
        /// </summary>
        public static RangeInteger Range(this int[] array)
        {
            int min = array[0];
            int max = array[0];

            for (int i = 1; i < array.Length; i++)
            {
                if (min > array[i])
                    min = array[i];
                if (max < array[i])
                    max = array[i];
            }
            return new RangeInteger(min, max);
        }

        /// <summary>
        ///   Gets the range of the values accross the columns of a matrix.
        /// </summary>
        public static RangeDouble[] Range(double[,] value)
        {
            RangeDouble[] ranges = new RangeDouble[value.GetLength(1)];

            for (int j = 0; j < ranges.Length; j++)
            {
                double max = value[0, j];
                double min = value[0, j];

                for (int i = 0; i < value.GetLength(0); i++)
                {
                    if (value[i, j] > max)
                        max = value[i, j];

                    if (value[i, j] < min)
                        min = value[i, j];
                }

                ranges[j] = new RangeDouble(min, max);
            }

            return ranges;
        }

        #endregion
        #region elementwise

        /// <summary>
        ///   Elementwise absolute value.
        /// </summary>
        public static int[] Abs(this int[] x)
        {
            int[] r = new int[x.Length];
            for (int i = 0; i < x.Length; i++)
                r[i] = System.Math.Abs(x[i]);
            return r;
        }

        /// <summary>
        ///   Elementwise absolute value.
        /// </summary>
        public static double[] Abs(this double[] x)
        {
            double[] r = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
                r[i] = System.Math.Abs(x[i]);
            return r;
        }

        /// <summary>
        ///   Elementwise absolute value.
        /// </summary>
        public static double[,] Abs(this double[,] x)
        {
            double[,] r = new double[x.GetLength(0), x.GetLength(1)];
            for (int i = 0; i < x.GetLength(0); i++)
                for (int j = 0; j < x.GetLength(1); j++)
                    r[i, j] = System.Math.Abs(x[i, j]);
            return r;
        }

        /// <summary>
        ///   Elementwise Square root.
        /// </summary>
        public static double[] Sqrt(this double[] x)
        {
            double[] r = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
                r[i] = System.Math.Sqrt(x[i]);
            return r;
        }

        /// <summary>
        ///   Elementwise power operation.
        /// </summary>
        /// <param name="a">A matrix.</param>
        /// <param name="p">A power.</param>
        public static double[,] ElementwisePower(this double[,] a, double p)
        {
            double[,] r = new double[a.GetLength(0), a.GetLength(1)];

            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < a.GetLength(1); j++)
                    r[i, j] = System.Math.Pow(a[i, j], p);

            return r;
        }

        /// <summary>
        ///   Elementwise power operation.
        /// </summary>
        /// <param name="a">A matrix.</param>
        /// <param name="p">A power.</param>
        public static double[] ElementwisePower(this double[] a, double p)
        {
            double[] r = new double[a.Length];

            for (int i = 0; i < r.Length; i++)
                r[i] = System.Math.Pow(a[i], p);

            return r;
        }

        /// <summary>
        ///   Elementwise divide operation.
        /// </summary>
        public static double[] ElementwiseDivide(this double[] a, double[] b)
        {
            double[] r = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
                r[i] = a[i] / b[i];

            return r;
        }

        /// <summary>
        ///   Elementwise divide operation.
        /// </summary>
        public static double[,] ElementwiseDivide(this double[,] a, double[,] b)
        {
            var r = new double[a.GetLength(0), a.GetLength(1)];

            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < b.GetLength(1); j++)
                {
                    r[i, j] = a[i, j] / b[i, j];
                }
            }

            return r;
        }

        /// <summary>
        ///   Elementwise multiply operation.
        /// </summary>
        public static double[] ElementwiseMultiply(this double[] a, double[] b)
        {
            double[] r = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
                r[i] = a[i] * b[i];

            return r;
        }

        /// <summary>
        ///   Elementwise multiply operation.
        /// </summary>
        public static double[,] ElementwiseMultiply(this double[,] a, double[,] b)
        {
            var r = new double[a.GetLength(0), a.GetLength(1)];

            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < b.GetLength(1); j++)
                {
                    r[i, j] = a[i, j] * b[i, j];
                }
            }

            return r;
        }

        /// <summary>
        ///   Elementwise multiply operation.
        /// </summary>
        public static int[] ElementwiseMultiply(this int[] a, int[] b)
        {
            var r = new int[a.Length];

            for (int i = 0; i < a.Length; i++)
                r[i] = a[i] * b[i];

            return r;
        }

        /// <summary>
        ///   Elementwise multiply operation.
        /// </summary>
        public static int[,] ElementwiseMultiply(this int[,] a, int[,] b)
        {
            var r = new int[a.GetLength(0), a.GetLength(1)];

            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < b.GetLength(1); j++)
                {
                    r[i, j] = a[i, j] * b[i, j];
                }
            }

            return r;
        }

        /// <summary>
        /// Rounds every element of a matrix up to the given decimal places.
        /// </summary>
        public static double[,] Round(this double[,] a, int decimals)
        {
            double[,] r = new double[a.GetLength(0), a.GetLength(1)];

            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < a.GetLength(1); j++)
                {
                    r[i, j] = System.Math.Round(a[i, j], decimals);
                }
            }

            return r;
        }

        #endregion
        #region conversions

        /// <summary>
        ///   Converts a jagged-array into a multidimensional array.
        /// </summary>
        public static T[,] ToMatrix<T>(this T[][] array)
        {
            int rows = array.Length;
            if (rows == 0) return new T[rows, 0];
            int cols = array[0].Length;

            T[,] m = new T[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    m[i, j] = array[i][j];

            return m;
        }

        /// <summary>
        ///   Converts an array into a multidimensional array.
        /// </summary>
        public static T[,] ToMatrix<T>(this T[] array)
        {
            T[,] m = new T[1, array.Length];
            for (int i = 0; i < array.Length; i++)
                m[0, i] = array[i];

            return m;
        }

        /// <summary>
        ///   Converts a multidimensional array into a jagged-array.
        /// </summary>
        public static T[][] ToArray<T>(this T[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            T[][] array = new T[rows][];
            for (int i = 0; i < rows; i++)
                array[i] = matrix.GetRow(i);

            return array;
        }

        /// <summary>
        ///   Converts a DataTable to a double[,] array.
        /// </summary>
        public static double[,] ToMatrix(this DataTable table, out string[] columnNames)
        {
            double[,] m = new double[table.Rows.Count, table.Columns.Count];
            columnNames = new string[table.Columns.Count];

            for (int j = 0; j < table.Columns.Count; j++)
            {
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    if (table.Columns[j].DataType == typeof(System.String))
                    {
                        m[i, j] = Double.Parse((String)table.Rows[i][j]);
                    }
                    else if (table.Columns[j].DataType == typeof(System.Boolean))
                    {
                        m[i, j] = (Boolean)table.Rows[i][j] ? 1.0 : 0.0;
                    }
                    else
                    {
                        m[i, j] = (Double)table.Rows[i][j];
                    }
                }

                columnNames[j] = table.Columns[j].Caption;
            }
            return m;
        }

        /// <summary>
        ///   Converts a DataTable to a double[,] array.
        /// </summary>
        public static double[,] ToMatrix(this DataTable table)
        {
            String[] names;
            return ToMatrix(table, out names);
        }

        /// <summary>
        ///   Converts a DataTable to a double[][] array.
        /// </summary>
        public static double[][] ToArray(this DataTable table)
        {
            double[][] m = new double[table.Rows.Count][];

            for (int i = 0; i < table.Rows.Count; i++)
            {
                m[i] = new double[table.Columns.Count];

                for (int j = 0; j < table.Columns.Count; j++)
                {
                    if (table.Columns[j].DataType == typeof(System.String))
                    {
                        m[i][j] = Double.Parse((String)table.Rows[i][j]);
                    }
                    else if (table.Columns[j].DataType == typeof(System.Boolean))
                    {
                        m[i][j] = (Boolean)table.Rows[i][j] ? 1.0 : 0.0;
                    }
                    else
                    {
                        m[i][j] = (Double)table.Rows[i][j];
                    }
                }
            }
            return m;
        }

        /// <summary>
        ///   Converts a DataColumn to a double[] array.
        /// </summary>
        public static double[] ToArray(this DataColumn column)
        {
            double[] m = new double[column.Table.Rows.Count];

            for (int i = 0; i < m.Length; i++)
            {
                object b = column.Table.Rows[i][column];

                if (column.DataType == typeof(System.String))
                {
                    m[i] = Double.Parse((String)b);
                }
                else if (column.DataType == typeof(System.Boolean))
                {
                    m[i] = (Boolean)b ? 1.0 : 0.0;
                }
                else
                {
                    m[i] = (Double)b;
                }
            }

            return m;
        }

        #endregion
        #region invert and solve

        /// <summary>
        ///   Returns the LHS solution vector if the matrix is square or the least squares solution otherwise.
        /// </summary>
        /// <remarks>
        ///   Please note that this does not check if the matrix is non-singular before attempting to solve.
        /// </remarks>
        public static double[,] Solve(this double[,] m, double[,] rightSide)
        {
            if (m.GetLength(0) == m.GetLength(1))
            {
                // Solve by LU Decomposition if matrix is square.
                return new LuDecomposition(m).Solve(rightSide);
            }
            else
            {
                // Solve by QR Decomposition if not.
                return new QrDecomposition(m).Solve(rightSide);
            }
        }

        /// <summary>
        ///   Inverse of the matrix if matrix is square, pseudoinverse otherwise.
        /// </summary>
        public static double[,] Inverse(this double[,] m)
        {
            return m.Solve(MMath.Diagonal(m.GetLength(0), 1.0));
        }

        /// <summary>
        ///   Computes the pseudo-inverse of a matrix.
        /// </summary>
        public static double[,] PseudoInverse(this double[,] m)
        {
            SingularValueDecomposition svd = new SingularValueDecomposition(m);
            return svd.Solve(MMath.Diagonal(m.GetLength(0), m.GetLength(1), 1.0));
        }

        #endregion
        #region other

        /// <summary>
        ///   Transforms a vector into a matrix of given dimensions.
        /// </summary>
        public static T[,] Reshape<T>(T[] array, int rows, int cols)
        {
            T[,] r = new T[rows, cols];

            for (int j = 0, k = 0; j < cols; j++)
                for (int i = 0; i < rows; i++)
                    r[i, j] = array[k++];

            return r;
        }

        #endregion

        #endregion
        #region statistics

        #region array

        /// <summary>Computes the Mean of the given values.</summary>
        /// <param name="vector">A double array containing the vector members.</param>
        /// <returns>The mean of the given data.</returns>
        public static double Mean(this double[] values)
        {
            double sum = 0.0;
            double n = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                sum += values[i];
            }
            return sum / n;
        }

        /// <summary>Computes the Standard Deviation of the given values.</summary>
        /// <param name="vector">A double array containing the vector members.</param>
        /// <returns>The standard deviation of the given data.</returns>
        public static double StandardDeviation(this double[] values)
        {
            return StandardDeviation(values, Mean(values));
        }

        /// <summary>Computes the Standard Deviation of the given values.</summary>
        /// <param name="vector">A double array containing the vector members.</param>
        /// <param name="mean">The mean of the vector, if already known.</param>
        /// <returns>The standard deviation of the given data.</returns>
        public static double StandardDeviation(this double[] values, double mean)
        {
            return System.Math.Sqrt(Variance(values, mean));
        }

        /// <summary>
        ///   Computes the Standard Error for a sample size, which estimates the
        ///   standard deviation of the sample mean based on the population mean.
        /// </summary>
        /// <param name="samples">The sample size.</param>
        /// <param name="standardDeviation">The sample standard deviation.</param>
        /// <returns>The standard error for the sample.</returns>
        public static double StandardError(int samples, double standardDeviation)
        {
            return standardDeviation / System.Math.Sqrt(samples);
        }

        /// <summary>
        ///   Computes the Standard Error for a sample size, which estimates the
        ///   standard deviation of the sample mean based on the population mean.
        /// </summary>
        /// <param name="vector">A double array containing the samples.</param>
        /// <returns>The standard error for the sample.</returns>
        public static double StandardError(double[] values)
        {
            return StandardError(values.Length, StandardDeviation(values));
        }

        /// <summary>Computes the Median of the given values.</summary>
        /// <param name="vector">A double array containing the vector members.</param>
        /// <returns>The median of the given data.</returns>
        public static double Median(double[] values)
        {
            return Median(values, false);
        }

        /// <summary>Computes the Median of the given values.</summary>
        /// <param name="values">An integer array containing the vector members.</param>
        /// <param name="alreadySorted">A boolean parameter informing if the given values have already been sorted.</param>
        /// <returns>The median of the given data.</returns>
        public static double Median(double[] values, bool alreadySorted)
        {
            double[] data = new double[values.Length];
            values.CopyTo(data, 0); // Creates a copy of the given values,

            if (!alreadySorted) // So we can sort it without modifying the original array.
                Array.Sort(data);

            int N = data.Length;

            if ((N % 2) == 0)
                return (data[N / 2] + data[(N / 2) + 1]) * 0.5; // N is even 
            else return data[(N + 1) / 2];                      // N is odd
        }

        /// <summary>Computes the Variance of the given values.</summary>
        /// <param name="vector">A double precision number array containing the vector members.</param>
        /// <returns>The variance of the given data.</returns>
        public static double Variance(double[] values)
        {
            return Variance(values, Mean(values));
        }

        /// <summary>Computes the Variance of the given values.</summary>
        /// <param name="vector">A number array containing the vector members.</param>
        /// <param name="mean">The mean of the array, if already known.</param>
        /// <returns>The variance of the given data.</returns>
        public static double Variance(double[] values, double mean)
        {
            double sum1 = 0.0;
            double sum2 = 0.0;
            double N = values.Length;
            double x = 0.0;

            for (int i = 0; i < values.Length; i++)
            {
                x = values[i] - mean;
                sum1 += x;
                sum2 += x * x;
            }

            // Sample variance
            return (sum2 - ((sum1 * sum1) / N)) / (N - 1);
        }

        /// <summary>Computes the Mode of the given values.</summary>
        /// <param name="values">A number array containing the vector values.</param>
        /// <returns>The variance of the given data.</returns>
        public static double Mode(double[] values)
        {
            int[] itemCount = new int[values.Length];
            double[] itemArray = new double[values.Length];
            int count = 0;

            for (int i = 0; i < values.Length; i++)
            {
                int index = Array.IndexOf<double>(itemArray, values[i], 0, count);

                if (index >= 0)
                {
                    itemCount[index]++;
                }
                else
                {
                    itemArray[count] = values[i];
                    itemCount[count] = 1;
                    count++;
                }
            }

            int maxValue = 0;
            int maxIndex = 0;

            for (int i = 0; i < count; i++)
            {
                if (itemCount[i] > maxValue)
                {
                    maxValue = itemCount[i];
                    maxIndex = i;
                }
            }

            return itemArray[maxIndex];
        }

        /// <summary>Computes the Covariance between two values arrays.</summary>
        /// <param name="u">A number array containing the first vector members.</param>
        /// <param name="v">A number array containing the second vector members.</param>
        /// <returns>The variance of the given data.</returns>
        public static double Covariance(double[] u, double[] v)
        {
            if (u.Length != v.Length)
            {
                throw new ArgumentException("Vector sizes must be equal.", "u");
            }

            double uSum = 0.0;
            double vSum = 0.0;
            double N = u.Length;

            // Calculate Sums for each vector
            for (int i = 0; i < u.Length; i++)
            {
                uSum += u[i];
                vSum += v[i];
            }

            double uMean = uSum / N;
            double vMean = vSum / N;

            double covariance = 0.0;
            for (int i = 0; i < u.Length; i++)
            {
                covariance += (u[i] - uMean) * (v[i] - vMean);
            }

            return covariance / (N - 1); // sample variance
        }

        /// <summary>
        ///   Computes the Skewness for the given values.
        /// </summary>
        /// <remarks>
        ///   Skewness characterizes the degree of asymmetry of a distribution
        ///   around its mean. Positive skewness indicates a distribution with
        ///   an asymmetric tail extending towards more positive values. Negative
        ///   skewness indicates a distribution with an asymmetric tail extending
        ///   towards more negative values.
        /// </remarks>
        /// <param name="values">A number array containing the vector values.</param>
        /// <returns>The skewness of the given data.</returns>
        public static double Skewness(double[] values)
        {
            double mean = Mean(values);
            return Skewness(values, mean, StandardDeviation(values, mean));
        }

        /// <summary>
        ///   Computes the Skewness for the given values.
        /// </summary>
        /// <remarks>
        ///   Skewness characterizes the degree of asymmetry of a distribution
        ///   around its mean. Positive skewness indicates a distribution with
        ///   an asymmetric tail extending towards more positive values. Negative
        ///   skewness indicates a distribution with an asymmetric tail extending
        ///   towards more negative values.
        /// </remarks>
        /// <param name="values">A number array containing the vector values.</param>
        /// <param name="mean">The values' mean, if already known.</param>
        /// <param name="standardDeviation">The values' standard deviations, if already known.</param>
        /// <returns>The skewness of the given data.</returns>
        public static double Skewness(double[] values, double mean, double standardDeviation)
        {
            int n = values.Length;
            double sum = 0.0;
            for (int i = 0; i < n; i++)
            {
                // Sum of third moment deviations
                sum += System.Math.Pow(values[i] - mean, 3);
            }

            return sum / ((n - 1) * System.Math.Pow(standardDeviation, 3));
        }

        public static double Kurtosis(double[] values)
        {
            double mean = Mean(values);
            return Kurtosis(values, mean, StandardDeviation(values, mean));
        }

        public static double Kurtosis(double[] values, double mean, double standardDeviation)
        {
            int n = values.Length;
            double sum = 0.0;
            for (int i = 0; i < n; i++)
            {
                // Sum of fourth moment deviations
                sum += System.Math.Pow(values[i] - mean, 4);
            }

            return sum / (n * System.Math.Pow(standardDeviation, 4)) - 3.0;
        }

        #endregion
        #region matrix

        /// <summary>Calculates the matrix Mean vector.</summary>
        /// <param name="m">A matrix whose means will be calculated.</param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        public static double[] Mean(double[,] value)
        {
            return Mean(value, 1);
        }

        /// <summary>Calculates the matrix Mean vector.</summary>
        /// <param name="m">A matrix whose means will be calculated.</param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        public static double[] Mean(double[,] value, int dimension)
        {
            if (dimension == 1)
            {
                double[] mean = new double[value.GetLength(1)];
                double rows = value.GetLength(0);

                // for each column
                for (int j = 0; j < value.GetLength(1); j++)
                {
                    // for each row
                    for (int i = 0; i < value.GetLength(0); i++)
                        mean[j] += value[i, j];

                    mean[j] = mean[j] / rows;
                }

                return mean;
            }
            else
            {
                double[] mean = new double[value.GetLength(0)];
                double cols = value.GetLength(1);

                // for each row
                for (int j = 0; j < value.GetLength(0); j++)
                {
                    // for each column
                    for (int i = 0; i < value.GetLength(1); i++)
                        mean[j] += value[j, i];

                    mean[j] = mean[j] / cols;
                }

                return mean;
            }
        }

        /// <summary>Calculates the matrix Mean vector.</summary>
        /// <param name="m">A matrix whose means will be calculated.</param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        public static double[] Mean(double[][] value)
        {
            double[] mean = new double[value.GetLength(1)];
            double rows = value.GetLength(0);

            // for each column
            for (int j = 0; j < value.GetLength(1); j++)
            {
                // for each row
                for (int i = 0; i < value.GetLength(0); i++)
                {
                    mean[j] += value[i][j];
                }

                mean[j] = mean[j] / rows;
            }

            return mean;
        }

        /// <summary>Calculates the matrix Mean vector.</summary>
        /// <param name="m">A matrix whose means will be calculated.</param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        public static double[] Mean(double[,] value, double[] sumVector)
        {
            double[] mean = new double[value.GetLength(1)];
            double rows = value.GetLength(0);

            // for each column
            for (int j = 0; j < value.GetLength(1); j++)
            {
                mean[j] = sumVector[j] / rows;
            }

            return mean;
        }

        /// <summary>Calculates the matrix Standard Deviations vector.</summary>
        /// <param name="m">A matrix whose deviations will be calculated.</param>
        /// <returns>Returns a vector containing the standard deviations of the given matrix.</returns>
        public static double[] StandardDeviation(double[,] value)
        {
            return StandardDeviation(value, Mean(value));
        }

        /// <summary>Calculates the matrix Standard Deviations vector.</summary>
        /// <param name="m">A matrix whose deviations will be calculated.</param>
        /// <param name="meanVector">The mean vector containing already calculated means for each column of the matix.</param>
        /// <returns>Returns a vector containing the standard deviations of the given matrix.</returns>
        public static double[] StandardDeviation(this double[,] value, double[] meanVector)
        {
            return MMath.Sqrt(Variance(value, meanVector));
        }

        /// <summary>Calculates the matrix Standard Deviations vector.</summary>
        /// <param name="m">A matrix whose deviations will be calculated.</param>
        /// <param name="meanVector">The mean vector containing already calculated means for each column of the matix.</param>
        /// <returns>Returns a vector containing the standard deviations of the given matrix.</returns>
        public static double[] StandardDeviation(this double[][] value, double[] meanVector)
        {
            return MMath.Sqrt(Variance(value, meanVector));
        }

        /// <summary>Calculates the matrix Medians vector.</summary>
        /// <param name="m">A matrix whose deviations will be calculated.</param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        public static double[] Variance(this double[,] value)
        {
            return Variance(value, Mean(value));
        }

        /// <summary>Calculates the matrix Medians vector.</summary>
        /// <param name="m">A matrix whose deviations will be calculated.</param>
        /// /// <param name="meanVector">The mean vector containing already calculated means for each column of the matix.</param>
        /// <returns>Returns a vector containing the mean of the given matrix.</returns>
        public static double[] Variance(this double[,] value, double[] means)
        {
            double[] variance = new double[value.GetLength(1)];

            // for each column (for each variable)
            for (int j = 0; j < value.GetLength(1); j++)
            {
                double sum1 = 0.0;
                double sum2 = 0.0;
                double x = 0.0;
                double N = value.GetLength(0);

                // for each row (observation of the variable)
                for (int i = 0; i < value.GetLength(0); i++)
                {
                    x = value[i, j] - means[j];
                    sum1 += x;
                    sum2 += x * x;
                }

                // calculate the variance
                variance[j] = (sum2 - ((sum1 * sum1) / N)) / (N - 1);
            }

            return variance;
        }

        /// <summary>Calculates the matrix Medians vector.</summary>
        /// <param name="m">A matrix whose deviations will be calculated.</param>
        /// /// <param name="meanVector">The mean vector containing already calculated means for each column of the matix.</param>
        /// <returns>Returns a vector containing the mean of the given matrix.</returns>
        public static double[] Variance(this double[][] value, double[] means)
        {
            double[] variance = new double[value[0].Length];

            // for each column (for each variable)
            for (int j = 0; j < value.GetLength(1); j++)
            {
                double sum1 = 0.0;
                double sum2 = 0.0;
                double x = 0.0;
                double N = value.GetLength(0);

                // for each row (observation of the variable)
                for (int i = 0; i < value.GetLength(0); i++)
                {
                    x = value[i][j] - means[j];
                    sum1 += x;
                    sum2 += x * x;
                }

                // calculate the variance
                variance[j] = (sum2 - ((sum1 * sum1) / N)) / (N - 1);
            }

            return variance;
        }

        /// <summary>Calculates the matrix Medians vector.</summary>
        /// <param name="m">A matrix whose deviations will be calculated.</param>
        /// <returns>Returns a vector containing the medians of the given matrix.</returns>
        public static double[] Median(double[,] value)
        {
            int rows = value.GetLength(0);
            int cols = value.GetLength(1);
            double[] medians = new double[cols];

            for (int i = 0; i < cols; i++)
            {
                double[] data = new double[rows];

                // Creates a copy of the given values
                for (int j = 0; j < rows; j++)
                    data[j] = value[j, i];

                Array.Sort(data); // Sort it

                int N = data.Length;

                if ((N % 2) == 0)
                    medians[i] = (data[N / 2] + data[(N / 2) + 1]) * 0.5; // N is even 
                else medians[i] = data[(N + 1) / 2];                      // N is odd
            }

            return medians;
        }

        /// <summary>Calculates the matrix Modes vector.</summary>
        /// <param name="m">A matrix whose modes will be calculated.</param>
        /// <returns>Returns a vector containing the modes of the given matrix.</returns>
        public static double[] Mode(this double[,] matrix)
        {
            double[] mode = new double[matrix.GetLength(1)];

            for (int i = 0; i < mode.Length; i++)
            {
                int[] itemCount = new int[matrix.GetLength(0)];
                double[] itemArray = new double[matrix.GetLength(0)];
                int count = 0;

                // for each row
                for (int j = 0; j < matrix.GetLength(0); j++)
                {
                    int index = Array.IndexOf<double>(itemArray, matrix[j, i], 0, count);

                    if (index >= 0)
                    {
                        itemCount[index]++;
                    }
                    else
                    {
                        itemArray[count] = matrix[j, i];
                        itemCount[count] = 1;
                        count++;
                    }
                }

                int maxValue = 0;
                int maxIndex = 0;

                for (int j = 0; j < count; j++)
                {
                    if (itemCount[j] > maxValue)
                    {
                        maxValue = itemCount[j];
                        maxIndex = j;
                    }
                }

                mode[i] = itemArray[maxIndex];
            }

            return mode;
        }

        /// <summary>
        ///   Computes the Skewness for the given values.
        /// </summary>
        /// <remarks>
        ///   Skewness characterizes the degree of asymmetry of a distribution
        ///   around its mean. Positive skewness indicates a distribution with
        ///   an asymmetric tail extending towards more positive values. Negative
        ///   skewness indicates a distribution with an asymmetric tail extending
        ///   towards more negative values.
        /// </remarks>
        /// <param name="values">A number array containing the vector values.</param>
        /// <returns>The skewness of the given data.</returns>
        public static double[] Skewness(double[,] matrix)
        {
            double[] means = Mean(matrix);
            return Skewness(matrix, means, StandardDeviation(matrix, means));
        }

        /// <summary>
        ///   Computes the Skewness for the given values.
        /// </summary>
        /// <remarks>
        ///   Skewness characterizes the degree of asymmetry of a distribution
        ///   around its mean. Positive skewness indicates a distribution with
        ///   an asymmetric tail extending towards more positive values. Negative
        ///   skewness indicates a distribution with an asymmetric tail extending
        ///   towards more negative values.
        /// </remarks>
        /// <param name="values">A number array containing the vector values.</param>
        /// <param name="mean">The values' mean, if already known.</param>
        /// <param name="standardDeviation">The values' standard deviations, if already known.</param>
        /// <returns>The skewness of the given data.</returns>
        public static double[] Skewness(double[,] matrix, double[] means, double[] standardDeviations)
        {
            int n = matrix.GetLength(0);
            double[] skewness = new double[matrix.GetLength(1)];
            for (int j = 0; j < skewness.Length; j++)
            {
                double sum = 0.0;
                for (int i = 0; i < n; i++)
                {
                    // Sum of third moment deviations
                    sum += System.Math.Pow(matrix[i, j] - means[j], 3);
                }

                skewness[j] = sum / ((n - 1) * System.Math.Pow(standardDeviations[j], 3));
            }

            return skewness;
        }

        public static double[] Kurtosis(double[,] matrix)
        {
            double[] means = Mean(matrix);
            return Kurtosis(matrix, means, StandardDeviation(matrix, means));
        }

        public static double[] Kurtosis(double[,] matrix, double[] means, double[] standardDeviations)
        {
            int n = matrix.GetLength(0);
            double[] kurtosis = new double[matrix.GetLength(1)];
            for (int j = 0; j < kurtosis.Length; j++)
            {
                double sum = 0.0;
                for (int i = 0; i < n; i++)
                {
                    // Sum of fourth moment deviations
                    sum += System.Math.Pow(matrix[i, j] - means[j], 4);
                }

                kurtosis[j] = sum / (n * System.Math.Pow(standardDeviations[j], 4)) - 3.0;
            }

            return kurtosis;
        }

        public static double[] StandardError(double[,] matrix)
        {
            return StandardError(matrix.GetLength(0), StandardDeviation(matrix));
        }

        public static double[] StandardError(int samples, double[] standardDeviations)
        {
            double[] standardErrors = new double[standardDeviations.Length];
            double sqrt = System.Math.Sqrt(samples);
            for (int i = 0; i < standardDeviations.Length; i++)
            {
                standardErrors[i] = standardDeviations[i] / sqrt;
            }
            return standardErrors;
        }

        /// <summary>Calculates the covariance matrix of a sample matrix, returning a new matrix object</summary>
        /// <remarks>
        ///   In statistics and probability theory, the covariance matrix is a matrix of
        ///   covariances between elements of a vector. It is the natural generalization
        ///   to higher dimensions of the concept of the variance of a scalar-valued
        ///   random variable.
        /// </remarks>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Covariance(this double[,] matrix)
        {
            return Covariance(matrix, Mean(matrix));
        }

        public static double[,] Covariance(this double[,] matrix, double[] mean)
        {
            return Scatter(matrix, mean, matrix.GetLength(0) - 1, 1);
        }

        public static double[,] Scatter(double[,] matrix, double[] mean)
        {
            return Scatter(matrix, mean, 1.0, 1);
        }

        public static double[,] Scatter(double[,] matrix, double[] mean, double divide)
        {
            return Scatter(matrix, mean, divide, 1);
        }

        public static double[,] Scatter(double[,] matrix, double[] mean, int dimension)
        {
            return Scatter(matrix, mean, 1.0, dimension);
        }

        public static double[,] Scatter(double[,] matrix, double[] mean, double divide, int dimension)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double[,] cov;

            if (dimension == 1)
            {
                cov = new double[cols, cols];
                for (int i = 0; i < cols; i++)
                {
                    for (int j = i; j < cols; j++)
                    {
                        double s = 0.0;
                        for (int k = 0; k < rows; k++)
                            s += (matrix[k, j] - mean[j]) * (matrix[k, i] - mean[i]);
                        s /= divide;
                        cov[i, j] = s;
                        cov[j, i] = s;
                    }
                }
            }
            else
            {
                cov = new double[rows, rows];
                for (int i = 0; i < rows; i++)
                {
                    for (int j = i; j < rows; j++)
                    {
                        double s = 0.0;
                        for (int k = 0; k < cols; k++)
                            s += (matrix[j, k] - mean[j]) * (matrix[i, k] - mean[i]);
                        s /= divide;
                        cov[i, j] = s;
                        cov[j, i] = s;
                    }
                }
            }

            return cov;
        }

        /// <summary>Calculates the correlation matrix of this samples, returning a new matrix object</summary>
        /// <remarks>
        /// In statistics and probability theory, the correlation matrix is the same
        /// as the covariance matrix of the standardized random variables.
        /// </remarks>
        /// <returns>The correlation matrix</returns>
        public static double[,] Correlation(double[,] matrix)
        {
            double[] means = Mean(matrix);
            return Correlation(matrix, means, StandardDeviation(matrix, means));
        }

        /// <summary>
        ///   Calculates the correlation matrix of this samples, returning a new matrix object
        /// </summary>
        /// <remarks>
        ///   In statistics and probability theory, the correlation matrix is the same
        ///   as the covariance matrix of the standardized random variables.
        /// </remarks>
        /// <returns>The correlation matrix</returns>
        public static double[,] Correlation(double[,] matrix, double[] mean, double[] stdDev)
        {
            double[,] scores = ZScores(matrix, mean, stdDev);

            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double N = rows;
            double[,] cor = new double[cols, cols];
            for (int i = 0; i < cols; i++)
            {
                for (int j = i; j < cols; j++)
                {
                    double c = 0.0;
                    for (int k = 0; k < rows; k++)
                    {
                        c += scores[k, j] * scores[k, i];
                    }
                    c /= N - 1.0;
                    cor[i, j] = c;
                    cor[j, i] = c;
                }
            }

            return cor;
        }

        /// <summary>Generates the Standard Scores, also known as Z-Scores, the core from the given data.</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double[,] ZScores(double[,] value)
        {
            double[] mean = Mean(value);
            return ZScores(value, mean, StandardDeviation(value, mean));
        }

        public static double[,] ZScores(double[,] value, double[] means, double[] deviations)
        {
            double[,] m = (double[,])value.Clone();

            Center(m, means);
            Standardize(m, deviations);

            return m;
        }

        /// <summary>Centers column data, subtracting the empirical mean from each variable.</summary>
        /// <param name="m">A matrix where each column represent a variable and each row represent a observation.</param>
        public static void Center(double[,] value)
        {
            Center(value, Mean(value));
        }

        /// <summary>Centers column data, subtracting the empirical mean from each variable.</summary>
        /// <param name="m">A matrix where each column represent a variable and each row represent a observation.</param>
        public static void Center(double[,] value, double[] means)
        {
            for (int i = 0; i < value.GetLength(0); i++)
            {
                for (int j = 0; j < value.GetLength(1); j++)
                {
                    value[i, j] -= means[j];
                }
            }
        }

        /// <summary>Standardizes column data, removing the empirical standard deviation from each variable.</summary>
        /// <param name="m">A matrix where each column represent a variable and each row represent a observation.</param>
        /// <remarks>This method does not remove the empirical mean prior to execution.</remarks>
        public static void Standardize(double[,] value)
        {
            Standardize(value, StandardDeviation(value));
        }

        public static void Standardize(this double[,] value, double[] deviations)
        {
            for (int i = 0; i < value.GetLength(0); i++)
            {
                for (int j = 0; j < value.GetLength(1); j++)
                {
                    value[i, j] /= deviations[j];
                }
            }
        }

        #endregion
        #region median list extensions

        /// <summary>
        /// Retrieve / calculate the median value from a list.
        /// </summary>
        /// <param name="values">A list of numbers.</param>
        /// <returns>The median value.</returns>
        public static double Median(this List<int> values)
        {
            if (values.Count == 1) return values[0];
            return values.Count.IsOdd() ? values[((values.Count - 1) / 2) + 1] : (double)(values[(values.Count / 2) - 1] + values[(values.Count / 2)]) / 2d;
        }

        /// <summary>
        /// Retrieve / calculate the median value from a list.
        /// </summary>
        /// <param name="values">A list of numbers.</param>
        /// <returns>The median value.</returns>
        public static double Median(this List<long> values)
        {
            if (values.Count == 1) return values[0];
            return values.Count.IsOdd() ? values[((values.Count - 1) / 2) + 1] : (double)(values[values.Count / 2] + values[(values.Count / 2) - 1]) / 2d;
        }

        /// <summary>
        /// Retrieve / calculate the median value from a list.
        /// </summary>
        /// <param name="values">A list of numbers.</param>
        /// <returns>The median value.</returns>
        public static float Median(this List<float> values)
        {
            if (values.Count == 1) return values[0];
            return values.Count.IsOdd() ? values[((values.Count - 1) / 2) + 1] : (values[values.Count / 2] + values[(values.Count / 2) - 1]) / 2f;
        }

        /// <summary>
        /// Retrieve / calculate the median value from a list.
        /// </summary>
        /// <param name="values">A list of numbers.</param>
        /// <returns>The median value.</returns>
        public static double Median(this List<double> values)
        {
            if (values.Count == 1) return values[0];
            return values.Count.IsOdd() ? values[((values.Count - 1) / 2) + 1] : (values[values.Count / 2] + values[(values.Count / 2) - 1]) / 2d;
        }

        /// <summary>
        /// Retrieve / calculate the median value from a list.
        /// </summary>
        /// <param name="values">A list of numbers.</param>
        /// <returns>The median value.</returns>
        public static decimal Median(this List<decimal> values)
        {
            if (values.Count == 1) return values[0];
            return values.Count.IsOdd() ? values[((values.Count - 1) / 2) + 1] : (decimal)(values[values.Count / 2] + values[(values.Count / 2) - 1]) / 2m;
        }

        #endregion
        #region gauss

        /// <summary>
        /// Calculate the parameters a, b and c for the Gaussian function
        /// </summary>
        /// <param name="my">The µ parameter (expected value)</param>
        /// <param name="sigmaSquared">The σ² parameter (variance)</param>
        /// <returns>A Tuple containing the three values in alphabetical order</returns>
        public static Tuple<double, double, double> GaussianFunctionParameters(double my, double sigmaSquared)
        {
            double c = Math.Sqrt(sigmaSquared);
            double a = 1 / (c * Math.Sqrt(2 * Math.PI));
            double b = my;
            return new Tuple<double, double, double>(a, b, c);
        }

        /// <summary>
        /// Calculates a value for a gaussian curve.
        /// </summary>
        /// <param name="x">The position on the X-axis to evaluate.</param>
        /// <param name="my">The µ parameter (expected value)</param>
        /// <param name="sigmaSquared">The σ² parameter (variance)</param>
        /// <returns>Value for a gaussian curve at X-axis position x</returns>
        public static double Gaussian(double x, double my, double sigmaSquared)
        {
            double c = Math.Sqrt(sigmaSquared);
            return Gaussian(x, 1 / (c * Math.Sqrt(2 * Math.PI)), my, c);
        }

        /// <summary>
        /// Calculates a value for a gaussian curve.
        /// </summary>
        /// <param name="x">The position on the X-axis to evaluate.</param>
        /// <param name="a">a = 1/(σ√(2π))</param>
        /// <param name="b">µ, expected value</param>
        /// <param name="c">σ, square root of the variance</param>
        /// <returns>Value for a gaussian curve at X-axis position x</returns>
        public static double Gaussian(double x, double a, double b, double c)
        {
            return a * Math.Exp(-(Math.Pow(x - b, 2) / (2 * Math.Pow(c, 2))));
        }

        #endregion
        #region summarizing, grouping and extending

        /// <summary>
        ///   Calculate the prevalence of a class.
        /// </summary>
        /// <param name="positives">An array of counts detailing the occurence of the first class.</param>
        /// <param name="negatives">An array of counts detailing the occurence of the second class.</param>
        /// <returns>An array containing the proportion of the first class over the total of occurances.</returns>
        public static double[] Proportions(int[] positives, int[] negatives)
        {
            double[] r = new double[positives.Length];
            for (int i = 0; i < r.Length; i++)
                r[i] = (double)positives[i] / (positives[i] + negatives[i]);
            return r;
        }

        /// <summary>
        ///   Calculate the prevalence of a class.
        /// </summary>
        /// <param name="data">A matrix containing counted, grouped data.</param>
        /// <param name="positiveColumn">The index for the column which contains counts for occurence of the first class.</param>
        /// <param name="negativeColumn">The index for the column which contains counts for occurence of the second class.</param>
        /// <returns>An array containing the proportion of the first class over the total of occurances.</returns>
        public static double[] Proportions(int[][] data, int positiveColumn, int negativeColumn)
        {
            double[] r = new double[data.Length];
            for (int i = 0; i < r.Length; i++)
                r[i] = (double)data[i][positiveColumn] / (data[i][positiveColumn] + data[i][negativeColumn]);
            return r;
        }

        /// <summary>
        ///   Groups the occurances contained in data matrix of binary (dichotomous) data.
        /// </summary>
        /// <param name="data">A data matrix containing at least a column of binary data.</param>
        /// <param name="labelColumn">Index of the column which contains the group label name.</param>
        /// <param name="dataColumn">Index of the column which contains the binary [0,1] data.</param>
        /// <returns>
        ///    A matrix containing the group label in the first column, the number of occurances of the first class
        ///    in the second column and the number of occurances of the second class in the third column.
        /// </returns>
        public static int[][] Group(int[][] data, int labelColumn, int dataColumn)
        {
            var groups = new List<int>();
            var groupings = new List<int[]>();

            for (int i = 0; i < data.Length; i++)
            {
                int group = data[i][labelColumn];
                if (!groups.Contains(group))
                {
                    groups.Add(group);

                    int positives = 0, negatives = 0;
                    for (int j = 0; j < data.Length; j++)
                    {
                        if (data[j][labelColumn] == group)
                        {
                            if (data[j][dataColumn] == 0)
                                negatives++;
                            else positives++;
                        }
                    }

                    groupings.Add(new int[] { group, positives, negatives });
                }
            }

            return groupings.ToArray();
        }

        /// <summary>
        ///   Extendes a grouped data into a full observation matrix.
        /// </summary>
        /// <param name="group">The group labels.</param>
        /// <param name="positives">
        ///   An array containing he occurence of the positive class
        ///   for each of the groups.</param>
        /// <param name="negatives">
        ///   An array containing he occurence of the negative class
        ///   for each of the groups.</param>
        /// <returns>A full sized observation matrix.</returns>
        public static int[][] Extend(int[] group, int[] positives, int[] negatives)
        {
            List<int[]> rows = new List<int[]>();

            for (int i = 0; i < group.Length; i++)
            {
                for (int j = 0; j < positives[i]; j++)
                    rows.Add(new int[] { group[i], 1 });

                for (int j = 0; j < negatives[i]; j++)
                    rows.Add(new int[] { group[i], 0 });
            }

            return rows.ToArray();
        }

        /// <summary>
        ///   Extendes a grouped data into a full observation matrix.
        /// </summary>
        /// <param name="data">The grouped data matrix.</param>
        /// <param name="labelColumn">Index of the column which contains the labels
        /// in the grouped data matrix. </param>
        /// <param name="positiveColumn">Index of the column which contains
        ///   the occurances for the first class.</param>
        /// <param name="positiveColumn">Index of the column which contains
        ///   the occurances for the second class.</param>
        /// <returns>A full sized observation matrix.</returns>
        public static int[][] Extend(int[][] data, int labelColumn, int positiveColumn, int negativeColumn)
        {
            List<int[]> rows = new List<int[]>();

            for (int i = 0; i < data.Length; i++)
            {
                for (int j = 0; j < data[i][positiveColumn]; j++)
                    rows.Add(new int[] { data[i][labelColumn], 1 });

                for (int j = 0; j < data[i][negativeColumn]; j++)
                    rows.Add(new int[] { data[i][labelColumn], 0 });
            }

            return rows.ToArray();
        }

        #endregion
        #region performance

        /// <summary>
        ///   Gets the coefficient of determination, as known as the R-Squared (R²)
        /// </summary>
        /// <remarks>
        ///    The coefficient of determination is used in the context of statistical models
        ///    whose main purpose is the prediction of future outcomes on the basis of other
        ///    related information. It is the proportion of variability in a data set that
        ///    is accounted for by the statistical model. It provides a measure of how well
        ///    future outcomes are likely to be predicted by the model.
        ///    
        ///    The R^2 coefficient of determination is a statistical measure of how well the
        ///    regression approximates the real data points. An R^2 of 1.0 indicates that the
        ///    regression perfectly fits the data.
        /// </remarks>
        public static double Determination(double[] actual, double[] expected)
        {
            // R-squared = 100 * SS(regression) / SS(total)

            int N = actual.Length;
            double SSe = 0.0;
            double SSt = 0.0;
            double avg = 0.0;
            double d;

            // Calculate expected output mean
            for (int i = 0; i < N; i++)
                avg += expected[i];
            avg /= N;

            // Calculate SSe and SSt
            for (int i = 0; i < N; i++)
            {
                d = expected[i] - actual[i];
                SSe += d * d;

                d = expected[i] - avg;
                SSt += d * d;
            }

            // Calculate R-Squared
            return 1.0 - (SSe / SSt);
        }

        #endregion

        #endregion
        #region special

        /// <summary>
        ///   Gamma function of the specified value.
        /// </summary>
        public static double Gamma(double x)
        {
            double[] P = {
						 1.60119522476751861407E-4,
						 1.19135147006586384913E-3,
						 1.04213797561761569935E-2,
						 4.76367800457137231464E-2,
						 2.07448227648435975150E-1,
						 4.94214826801497100753E-1,
						 9.99999999999999996796E-1
					 };
            double[] Q = {
						 -2.31581873324120129819E-5,
						 5.39605580493303397842E-4,
						 -4.45641913851797240494E-3,
						 1.18139785222060435552E-2,
						 3.58236398605498653373E-2,
						 -2.34591795718243348568E-1,
						 7.14304917030273074085E-2,
						 1.00000000000000000320E0
					 };

            double p, z;

            double q = System.Math.Abs(x);

            if (q > 33.0)
            {
                if (x < 0.0)
                {
                    p = System.Math.Floor(q);

                    if (p == q)
                        throw new OverflowException();

                    z = q - p;
                    if (z > 0.5)
                    {
                        p += 1.0;
                        z = q - p;
                    }
                    z = q * System.Math.Sin(System.Math.PI * z);

                    if (z == 0.0)
                        throw new OverflowException();

                    z = System.Math.Abs(z);
                    z = System.Math.PI / (z * Stirf(q));

                    return -z;
                }
                else
                {
                    return Stirf(x);
                }
            }

            z = 1.0;
            while (x >= 3.0)
            {
                x -= 1.0;
                z *= x;
            }

            while (x < 0.0)
            {
                if (x == 0.0)
                {
                    throw new ArithmeticException();
                }
                else if (x > -1.0E-9)
                {
                    return (z / ((1.0 + 0.5772156649015329 * x) * x));
                }
                z /= x;
                x += 1.0;
            }

            while (x < 2.0)
            {
                if (x == 0.0)
                {
                    throw new ArithmeticException();
                }
                else if (x < 1.0E-9)
                {
                    return (z / ((1.0 + 0.5772156649015329 * x) * x));
                }

                z /= x;
                x += 1.0;
            }

            if ((x == 2.0) || (x == 3.0)) return z;

            x -= 2.0;
            p = Polevl(x, P, 6);
            q = Polevl(x, Q, 7);
            return z * p / q;

        }

        /// <summary>
        ///   Regularized Gamma function (P)
        /// </summary>
        public static double Rgamma(double a, double z)
        {
            return Igam(a, z) / Gamma(a);
        }

        /// <summary>
        ///   Digamma function.
        /// </summary>
        public static double Digamma(double x)
        {
            double s = 0;
            double w = 0;
            double y = 0;
            double z = 0;
            double nz = 0;

            bool negative = false;

            if (x <= 0.0)
            {
                negative = true;
                double q = x;
                double p = (int)System.Math.Floor(q);

                if (p == q)
                    throw new OverflowException("digamma");

                nz = q - p;

                if (nz != 0.5)
                {
                    if (nz > 0.5)
                    {
                        p = p + 1.0;
                        nz = q - p;
                    }
                    nz = System.Math.PI / System.Math.Tan(System.Math.PI * nz);
                }
                else
                {
                    nz = 0.0;
                }

                x = 1.0 - x;
            }

            if (x <= 10.0 & x == System.Math.Floor(x))
            {
                y = 0.0;
                int n = (int)System.Math.Floor(x);
                for (int i = 1; i <= n - 1; i++)
                {
                    w = i;
                    y = y + 1.0 / w;
                }
                y = y - 0.57721566490153286061;
            }
            else
            {
                s = x;
                w = 0.0;

                while (s < 10.0)
                {
                    w = w + 1.0 / s;
                    s = s + 1.0;
                }

                if (s < 1.0E17)
                {
                    z = 1.0 / (s * s);

                    double polv = 8.33333333333333333333E-2;
                    polv = polv * z - 2.10927960927960927961E-2;
                    polv = polv * z + 7.57575757575757575758E-3;
                    polv = polv * z - 4.16666666666666666667E-3;
                    polv = polv * z + 3.96825396825396825397E-3;
                    polv = polv * z - 8.33333333333333333333E-3;
                    polv = polv * z + 8.33333333333333333333E-2;
                    y = z * polv;
                }
                else
                {
                    y = 0.0;
                }
                y = System.Math.Log(s) - 0.5 / s - y - w;
            }

            if (negative == true)
            {
                y = y - nz;
            }

            return y;
        }

        /// <summary>
        ///   Gamma function as computed by Stirling's formula.
        /// </summary>
        public static double Stirf(double x)
        {
            double[] STIR = {
							7.87311395793093628397E-4,
							-2.29549961613378126380E-4,
							-2.68132617805781232825E-3,
							3.47222221605458667310E-3,
							8.33333333333482257126E-2,
		};
            double MAXSTIR = 143.01608;

            double w = 1.0 / x;
            double y = System.Math.Exp(x);

            w = 1.0 + w * Polevl(w, STIR, 4);

            if (x > MAXSTIR)
            {
                double v = System.Math.Pow(x, 0.5 * x - 0.25);
                y = v * (v / y);
            }
            else
            {
                y = System.Math.Pow(x, x - 0.5) / y;
            }
            y = SqrtPi * y * w;
            return y;
        }

        /// <summary>
        ///   Complemented incomplete gamma function.
        /// </summary>
        public static double Igamc(double a, double x)
        {
            double big = 4.503599627370496e15;
            double biginv = 2.22044604925031308085e-16;
            double ans, ax, c, yc, r, t, y, z;
            double pk, pkm1, pkm2, qk, qkm1, qkm2;

            if (x <= 0 || a <= 0) return 1.0;

            if (x < 1.0 || x < a) return 1.0 - Igam(a, x);

            ax = a * System.Math.Log(x) - x - Lgamma(a);
            if (ax < -MaxLog) return 0.0;

            ax = System.Math.Exp(ax);

            /* continued fraction */
            y = 1.0 - a;
            z = x + y + 1.0;
            c = 0.0;
            pkm2 = 1.0;
            qkm2 = x;
            pkm1 = x + 1.0;
            qkm1 = z * x;
            ans = pkm1 / qkm1;

            do
            {
                c += 1.0;
                y += 1.0;
                z += 2.0;
                yc = y * c;
                pk = pkm1 * z - pkm2 * yc;
                qk = qkm1 * z - qkm2 * yc;
                if (qk != 0)
                {
                    r = pk / qk;
                    t = System.Math.Abs((ans - r) / r);
                    ans = r;
                }
                else
                    t = 1.0;

                pkm2 = pkm1;
                pkm1 = pk;
                qkm2 = qkm1;
                qkm1 = qk;
                if (System.Math.Abs(pk) > big)
                {
                    pkm2 *= biginv;
                    pkm1 *= biginv;
                    qkm2 *= biginv;
                    qkm1 *= biginv;
                }
            } while (t > Epsilon);

            return ans * ax;
        }

        /// <summary>
        ///   Incomplete gamma function.
        /// </summary>
        public static double Igam(double a, double x)
        {
            double ans, ax, c, r;

            if (x <= 0 || a <= 0) return 0.0;

            if (x > 1.0 && x > a) return 1.0 - Igamc(a, x);

            ax = a * System.Math.Log(x) - x - Lgamma(a);
            if (ax < -MaxLog) return (0.0);

            ax = System.Math.Exp(ax);

            r = a;
            c = 1.0;
            ans = 1.0;

            do
            {
                r += 1.0;
                c *= x / r;
                ans += c;
            } while (c / ans > Epsilon);

            return (ans * ax / a);

        }

        /// <summary>
        ///   Chi-square function (left hand tail).
        /// </summary>
        /// <remarks>
        ///   Returns the area under the left hand tail (from 0 to x)
        ///   of the Chi square probability density function with
        ///   df degrees of freedom.
        /// </remarks>
        /// <param name="df">degrees of freedom</param>
        /// <param name="x">double value</param>
        /// <returns></returns>
        public static double ChiSq(double df, double x)
        {
            if (x < 0.0 || df < 1.0) return 0.0;
            return Igam(df / 2.0, x / 2.0);
        }

        /// <summary>
        /// Chi-square function (right hand tail).
        /// </summary>
        /// <remarks>
        ///  Returns the area under the right hand tail (from x to
        ///  infinity) of the Chi square probability density function
        ///  with df degrees of freedom:
        /// </remarks>
        /// <param name="df">degrees of freedom</param>
        /// <param name="x">double value</param>
        /// <returns></returns>
        public static double ChiSqc(double df, double x)
        {
            if (x < 0.0 || df < 1.0) return 0.0;
            return Igamc(df / 2.0, x / 2.0);
        }

        /// <summary>
        /// Sum of the first k terms of the Poisson distribution.
        /// </summary>
        /// <param name="k">number of terms</param>
        /// <param name="x">double value</param>
        /// <returns></returns>
        public static double Poisson(int k, double x)
        {
            if (k < 0 || x < 0) return 0.0;
            return Igamc((double)(k + 1), x);
        }

        /// <summary>
        ///   Sum of the terms k+1 to infinity of the Poisson distribution.
        /// </summary>
        /// <param name="k">start</param>
        /// <param name="x">double value</param>
        /// <returns></returns>
        public static double Poissonc(int k, double x)
        {
            if (k < 0 || x < 0) return 0.0;
            return Igam((double)(k + 1), x);
        }

        /// <summary>
        ///   Area under the Gaussian probability density function,
        ///   integrated from minus infinity to the given value.
        /// </summary>
        /// <returns>
        ///   The area under the Gaussian p.d.f. integrated
        ///   from minus infinity to the given value.
        /// </returns>
        public static double Normal(double value)
        {
            double x, y, z;

            x = value * SqrtH;
            z = System.Math.Abs(x);

            if (z < SqrtH) y = 0.5 + 0.5 * Erf(x);
            else
            {
                y = 0.5 * Erfc(z);
                if (x > 0) y = 1.0 - y;
            }

            return y;
        }

        /// <summary>
        ///   Complementary error function of the specified value.
        /// </summary>
        /// <remarks>
        ///   http://mathworld.wolfram.com/Erfc.html
        /// </remarks>
        public static double Erfc(double value)
        {
            double x, y, z, p, q;

            double[] P = {
						 2.46196981473530512524E-10,
						 5.64189564831068821977E-1,
						 7.46321056442269912687E0,
						 4.86371970985681366614E1,
						 1.96520832956077098242E2,
						 5.26445194995477358631E2,
						 9.34528527171957607540E2,
						 1.02755188689515710272E3,
						 5.57535335369399327526E2
					 };
            double[] Q = {
						 //1.0
						 1.32281951154744992508E1,
						 8.67072140885989742329E1,
						 3.54937778887819891062E2,
						 9.75708501743205489753E2,
						 1.82390916687909736289E3,
						 2.24633760818710981792E3,
						 1.65666309194161350182E3,
						 5.57535340817727675546E2
					 };

            double[] R = {
						 5.64189583547755073984E-1,
						 1.27536670759978104416E0,
						 5.01905042251180477414E0,
						 6.16021097993053585195E0,
						 7.40974269950448939160E0,
						 2.97886665372100240670E0
					 };
            double[] S = {
						 //1.00000000000000000000E0, 
						 2.26052863220117276590E0,
						 9.39603524938001434673E0,
						 1.20489539808096656605E1,
						 1.70814450747565897222E1,
						 9.60896809063285878198E0,
						 3.36907645100081516050E0
					 };

            if (value < 0.0) x = -value;
            else x = value;

            if (x < 1.0) return 1.0 - Erf(value);

            z = -value * value;

            if (z < -MaxLog)
            {
                if (value < 0) return (2.0);
                else return (0.0);
            }

            z = System.Math.Exp(z);

            if (x < 8.0)
            {
                p = Polevl(x, P, 8);
                q = P1evl(x, Q, 8);
            }
            else
            {
                p = Polevl(x, R, 5);
                q = P1evl(x, S, 6);
            }

            y = (z * p) / q;

            if (value < 0) y = 2.0 - y;

            if (y == 0.0)
            {
                if (value < 0) return 2.0;
                else return (0.0);
            }


            return y;
        }

        /// <summary>
        ///   Error function of the specified value.
        /// </summary>
        public static double Erf(double x)
        {
            double y, z;
            double[] T = {
						 9.60497373987051638749E0,
						 9.00260197203842689217E1,
						 2.23200534594684319226E3,
						 7.00332514112805075473E3,
						 5.55923013010394962768E4
					 };
            double[] U = {
						 //1.00000000000000000000E0,
						 3.35617141647503099647E1,
						 5.21357949780152679795E2,
						 4.59432382970980127987E3,
						 2.26290000613890934246E4,
						 4.92673942608635921086E4
					 };

            if (System.Math.Abs(x) > 1.0)
                return (1.0 - Erfc(x));

            z = x * x;
            y = x * Polevl(z, T, 4) / P1evl(z, U, 5);

            return y;
        }

        /// <summary>
        ///   Natural logarithm of gamma function.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double Lgamma(double x)
        {
            double p, q, w, z;

            double[] A = {
						 8.11614167470508450300E-4,
						 -5.95061904284301438324E-4,
						 7.93650340457716943945E-4,
						 -2.77777777730099687205E-3,
						 8.33333333333331927722E-2
					 };
            double[] B = {
						 -1.37825152569120859100E3,
						 -3.88016315134637840924E4,
						 -3.31612992738871184744E5,
						 -1.16237097492762307383E6,
						 -1.72173700820839662146E6,
						 -8.53555664245765465627E5
					 };
            double[] C = {
						 /* 1.00000000000000000000E0, */
						 -3.51815701436523470549E2,
						 -1.70642106651881159223E4,
						 -2.20528590553854454839E5,
						 -1.13933444367982507207E6,
						 -2.53252307177582951285E6,
						 -2.01889141433532773231E6
					 };

            if (x < -34.0)
            {
                q = -x;
                w = Lgamma(q);
                p = System.Math.Floor(q);

                if (p == q)
                    throw new OverflowException("lgamma");

                z = q - p;
                if (z > 0.5)
                {
                    p += 1.0;
                    z = p - q;
                }
                z = q * System.Math.Sin(System.Math.PI * z);

                if (z == 0.0)
                    throw new OverflowException("lgamma");

                z = LogPi - System.Math.Log(z) - w;
                return z;
            }

            if (x < 13.0)
            {
                z = 1.0;
                while (x >= 3.0)
                {
                    x -= 1.0;
                    z *= x;
                }
                while (x < 2.0)
                {
                    if (x == 0.0)
                        throw new OverflowException("lgamma");

                    z /= x;
                    x += 1.0;
                }
                if (z < 0.0) z = -z;
                if (x == 2.0) return System.Math.Log(z);
                x -= 2.0;
                p = x * Polevl(x, B, 5) / P1evl(x, C, 6);
                return (System.Math.Log(z) + p);
            }

            if (x > 2.556348e305)
                throw new OverflowException("lgamma");

            q = (x - 0.5) * System.Math.Log(x) - x + 0.91893853320467274178;
            if (x > 1.0e8) return (q);

            p = 1.0 / (x * x);
            if (x >= 1000.0)
                q += ((7.9365079365079365079365e-4 * p
                    - 2.7777777777777777777778e-3) * p
                    + 0.0833333333333333333333) / x;
            else
                q += Polevl(p, A, 4) / x;
            return q;
        }

        /// <summary>
        ///   Incomplete beta function evaluated from zero to xx.
        /// </summary>
        public static double Ibeta(double aa, double bb, double xx)
        {
            double a, b, t, x, xc, w, y;
            bool flag;

            if (aa <= 0.0)
                throw new ArgumentOutOfRangeException("aa", "domain error");
            if (bb <= 0.0)
                throw new ArgumentOutOfRangeException("bb", "domain error");

            if ((xx <= 0.0) || (xx >= 1.0))
            {
                if (xx == 0.0) return 0.0;
                if (xx == 1.0) return 1.0;
                throw new ArgumentOutOfRangeException("xx", "domain error");
            }

            flag = false;
            if ((bb * xx) <= 1.0 && xx <= 0.95)
            {
                t = PowerSeries(aa, bb, xx);
                return t;
            }

            w = 1.0 - xx;

            if (xx > (aa / (aa + bb)))
            {
                flag = true;
                a = bb;
                b = aa;
                xc = xx;
                x = w;
            }
            else
            {
                a = aa;
                b = bb;
                xc = w;
                x = xx;
            }

            if (flag && (b * x) <= 1.0 && x <= 0.95)
            {
                t = PowerSeries(a, b, x);
                if (t <= Epsilon) t = 1.0 - Epsilon;
                else t = 1.0 - t;
                return t;
            }

            y = x * (a + b - 2.0) - (a - 1.0);
            if (y < 0.0)
                w = Incbcf(a, b, x);
            else
                w = Incbd(a, b, x) / xc;


            y = a * System.Math.Log(x);
            t = b * System.Math.Log(xc);
            if ((a + b) < MaxGamma && System.Math.Abs(y) < MaxLog && System.Math.Abs(t) < MaxLog)
            {
                t = System.Math.Pow(xc, b);
                t *= System.Math.Pow(x, a);
                t /= a;
                t *= w;
                t *= Gamma(a + b) / (Gamma(a) * Gamma(b));
                if (flag)
                {
                    if (t <= Epsilon) t = 1.0 - Epsilon;
                    else t = 1.0 - t;
                }
                return t;
            }

            y += t + Lgamma(a + b) - Lgamma(a) - Lgamma(b);
            y += System.Math.Log(w / a);
            if (y < MinLog)
                t = 0.0;
            else
                t = System.Math.Exp(y);

            if (flag)
            {
                if (t <= Epsilon) t = 1.0 - Epsilon;
                else t = 1.0 - t;
            }
            return t;
        }

        /// <summary>
        ///   Continued fraction expansion #1 for incomplete beta integral.
        /// </summary>
        public static double Incbcf(double a, double b, double x)
        {
            double xk, pk, pkm1, pkm2, qk, qkm1, qkm2;
            double k1, k2, k3, k4, k5, k6, k7, k8;
            double r, t, ans, thresh;
            int n;
            double big = 4.503599627370496e15;
            double biginv = 2.22044604925031308085e-16;

            k1 = a;
            k2 = a + b;
            k3 = a;
            k4 = a + 1.0;
            k5 = 1.0;
            k6 = b - 1.0;
            k7 = k4;
            k8 = a + 2.0;

            pkm2 = 0.0;
            qkm2 = 1.0;
            pkm1 = 1.0;
            qkm1 = 1.0;
            ans = 1.0;
            r = 1.0;
            n = 0;
            thresh = 3.0 * Epsilon;

            do
            {
                xk = -(x * k1 * k2) / (k3 * k4);
                pk = pkm1 + pkm2 * xk;
                qk = qkm1 + qkm2 * xk;
                pkm2 = pkm1;
                pkm1 = pk;
                qkm2 = qkm1;
                qkm1 = qk;

                xk = (x * k5 * k6) / (k7 * k8);
                pk = pkm1 + pkm2 * xk;
                qk = qkm1 + qkm2 * xk;
                pkm2 = pkm1;
                pkm1 = pk;
                qkm2 = qkm1;
                qkm1 = qk;

                if (qk != 0) r = pk / qk;
                if (r != 0)
                {
                    t = System.Math.Abs((ans - r) / r);
                    ans = r;
                }
                else
                    t = 1.0;

                if (t < thresh) return ans;

                k1 += 1.0;
                k2 += 1.0;
                k3 += 2.0;
                k4 += 2.0;
                k5 += 1.0;
                k6 -= 1.0;
                k7 += 2.0;
                k8 += 2.0;

                if ((System.Math.Abs(qk) + System.Math.Abs(pk)) > big)
                {
                    pkm2 *= biginv;
                    pkm1 *= biginv;
                    qkm2 *= biginv;
                    qkm1 *= biginv;
                }
                if ((System.Math.Abs(qk) < biginv) || (System.Math.Abs(pk) < biginv))
                {
                    pkm2 *= big;
                    pkm1 *= big;
                    qkm2 *= big;
                    qkm1 *= big;
                }
            } while (++n < 300);

            return ans;
        }

        /// <summary>
        ///   Continued fraction expansion #2 for incomplete beta integral.
        /// </summary>
        public static double Incbd(double a, double b, double x)
        {
            double xk, pk, pkm1, pkm2, qk, qkm1, qkm2;
            double k1, k2, k3, k4, k5, k6, k7, k8;
            double r, t, ans, z, thresh;
            int n;
            double big = 4.503599627370496e15;
            double biginv = 2.22044604925031308085e-16;

            k1 = a;
            k2 = b - 1.0;
            k3 = a;
            k4 = a + 1.0;
            k5 = 1.0;
            k6 = a + b;
            k7 = a + 1.0;
            ;
            k8 = a + 2.0;

            pkm2 = 0.0;
            qkm2 = 1.0;
            pkm1 = 1.0;
            qkm1 = 1.0;
            z = x / (1.0 - x);
            ans = 1.0;
            r = 1.0;
            n = 0;
            thresh = 3.0 * Epsilon;
            do
            {
                xk = -(z * k1 * k2) / (k3 * k4);
                pk = pkm1 + pkm2 * xk;
                qk = qkm1 + qkm2 * xk;
                pkm2 = pkm1;
                pkm1 = pk;
                qkm2 = qkm1;
                qkm1 = qk;

                xk = (z * k5 * k6) / (k7 * k8);
                pk = pkm1 + pkm2 * xk;
                qk = qkm1 + qkm2 * xk;
                pkm2 = pkm1;
                pkm1 = pk;
                qkm2 = qkm1;
                qkm1 = qk;

                if (qk != 0) r = pk / qk;
                if (r != 0)
                {
                    t = System.Math.Abs((ans - r) / r);
                    ans = r;
                }
                else
                    t = 1.0;

                if (t < thresh) return ans;

                k1 += 1.0;
                k2 -= 1.0;
                k3 += 2.0;
                k4 += 2.0;
                k5 += 1.0;
                k6 += 1.0;
                k7 += 2.0;
                k8 += 2.0;

                if ((System.Math.Abs(qk) + System.Math.Abs(pk)) > big)
                {
                    pkm2 *= biginv;
                    pkm1 *= biginv;
                    qkm2 *= biginv;
                    qkm1 *= biginv;
                }
                if ((System.Math.Abs(qk) < biginv) || (System.Math.Abs(pk) < biginv))
                {
                    pkm2 *= big;
                    pkm1 *= big;
                    qkm2 *= big;
                    qkm1 *= big;
                }
            } while (++n < 300);

            return ans;
        }

        /// <summary>
        ///   Power series for incomplete beta integral. Use when b*x
        ///   is small and x not too close to 1.
        /// </summary>
        public static double PowerSeries(double a, double b, double x)
        {
            double s, t, u, v, n, t1, z, ai;

            ai = 1.0 / a;
            u = (1.0 - b) * x;
            v = u / (a + 1.0);
            t1 = v;
            t = u;
            n = 2.0;
            s = 0.0;
            z = Epsilon * ai;
            while (System.Math.Abs(v) > z)
            {
                u = (n - b) * x / n;
                t *= u;
                v = t / (a + n);
                s += v;
                n += 1.0;
            }
            s += t1;
            s += ai;

            u = a * System.Math.Log(x);
            if ((a + b) < MaxGamma && System.Math.Abs(u) < MaxLog)
            {
                t = Gamma(a + b) / (Gamma(a) * Gamma(b));
                s = s * t * System.Math.Pow(x, a);
            }
            else
            {
                t = Lgamma(a + b) - Lgamma(a) - Lgamma(b) + u + System.Math.Log(s);
                if (t < MinLog) s = 0.0;
                else s = System.Math.Exp(t);
            }
            return s;
        }

        /// <summary>
        ///   Evaluates polynomial of degree N
        /// </summary>
        public static double Polevl(double x, double[] coef, int N)
        {
            double ans;

            ans = coef[0];

            for (int i = 1; i <= N; i++)
            {
                ans = ans * x + coef[i];
            }

            return ans;
        }

        /// <summary>
        ///   Evaluates polynomial of degree N with assumption that coef[N] = 1.0
        /// </summary>
        public static double P1evl(double x, double[] coef, int N)
        {
            double ans;

            ans = x + coef[0];

            for (int i = 1; i < N; i++)
            {
                ans = ans * x + coef[i];
            }

            return ans;
        }

        /// <summary>
        /// Returns the Bessel function of order 0 of the specified number.
        /// </summary>
        public static double BesselJ0(double x)
        {
            double ax;

            if ((ax = System.Math.Abs(x)) < 8.0)
            {
                double y = x * x;
                double ans1 = 57568490574.0 + y * (-13362590354.0 + y * (651619640.7
                    + y * (-11214424.18 + y * (77392.33017 + y * (-184.9052456)))));
                double ans2 = 57568490411.0 + y * (1029532985.0 + y * (9494680.718
                    + y * (59272.64853 + y * (267.8532712 + y * 1.0))));

                return ans1 / ans2;

            }
            else
            {
                double z = 8.0 / ax;
                double y = z * z;
                double xx = ax - 0.785398164;
                double ans1 = 1.0 + y * (-0.1098628627e-2 + y * (0.2734510407e-4
                    + y * (-0.2073370639e-5 + y * 0.2093887211e-6)));
                double ans2 = -0.1562499995e-1 + y * (0.1430488765e-3
                    + y * (-0.6911147651e-5 + y * (0.7621095161e-6
                    - y * 0.934935152e-7)));

                return System.Math.Sqrt(0.636619772 / ax) *
                    (System.Math.Cos(xx) * ans1 - z * System.Math.Sin(xx) * ans2);
            }
        }

        /// <summary>
        /// Returns the Bessel function of order 1 of the specified number.
        /// </summary>
        public static double BesselJ(double x)
        {
            double ax;
            double y;
            double ans1, ans2;

            if ((ax = System.Math.Abs(x)) < 8.0)
            {
                y = x * x;
                ans1 = x * (72362614232.0 + y * (-7895059235.0 + y * (242396853.1
                    + y * (-2972611.439 + y * (15704.48260 + y * (-30.16036606))))));
                ans2 = 144725228442.0 + y * (2300535178.0 + y * (18583304.74
                    + y * (99447.43394 + y * (376.9991397 + y * 1.0))));
                return ans1 / ans2;
            }
            else
            {
                double z = 8.0 / ax;
                double xx = ax - 2.356194491;
                y = z * z;

                ans1 = 1.0 + y * (0.183105e-2 + y * (-0.3516396496e-4
                    + y * (0.2457520174e-5 + y * (-0.240337019e-6))));
                ans2 = 0.04687499995 + y * (-0.2002690873e-3
                    + y * (0.8449199096e-5 + y * (-0.88228987e-6
                    + y * 0.105787412e-6)));
                double ans = System.Math.Sqrt(0.636619772 / ax) *
                    (System.Math.Cos(xx) * ans1 - z * System.Math.Sin(xx) * ans2);
                if (x < 0.0) ans = -ans;
                return ans;
            }
        }

        /// <summary>
        /// Returns the Bessel function of order n of the specified number.
        /// </summary>
        public static double BesselJ(int n, double x)
        {
            int j, m;
            double ax, bj, bjm, bjp, sum, tox, ans;
            bool jsum;

            double ACC = 40.0;
            double BIGNO = 1.0e+10;
            double BIGNI = 1.0e-10;

            if (n == 0) return BesselJ0(x);
            if (n == 1) return BesselJ(x);

            ax = System.Math.Abs(x);
            if (ax == 0.0) return 0.0;
            else if (ax > (double)n)
            {
                tox = 2.0 / ax;
                bjm = BesselJ0(ax);
                bj = BesselJ(ax);
                for (j = 1; j < n; j++)
                {
                    bjp = j * tox * bj - bjm;
                    bjm = bj;
                    bj = bjp;
                }
                ans = bj;
            }
            else
            {
                tox = 2.0 / ax;
                m = 2 * ((n + (int)System.Math.Sqrt(ACC * n)) / 2);
                jsum = false;
                bjp = ans = sum = 0.0;
                bj = 1.0;
                for (j = m; j > 0; j--)
                {
                    bjm = j * tox * bj - bjp;
                    bjp = bj;
                    bj = bjm;
                    if (System.Math.Abs(bj) > BIGNO)
                    {
                        bj *= BIGNI;
                        bjp *= BIGNI;
                        ans *= BIGNI;
                        sum *= BIGNI;
                    }
                    if (jsum) sum += bj;
                    jsum = !jsum;
                    if (j == n) ans = bjp;
                }
                sum = 2.0 * sum - bj;
                ans /= sum;
            }
            return x < 0.0 && n % 2 == 1 ? -ans : ans;
        }

        /// <summary>
        /// Returns the Bessel function of the second kind, of order 0 of the specified number.
        /// </summary>
        public static double BesselY0(double x)
        {
            if (x < 8.0)
            {
                double y = x * x;

                double ans1 = -2957821389.0 + y * (7062834065.0 + y * (-512359803.6
                    + y * (10879881.29 + y * (-86327.92757 + y * 228.4622733))));
                double ans2 = 40076544269.0 + y * (745249964.8 + y * (7189466.438
                    + y * (47447.26470 + y * (226.1030244 + y * 1.0))));

                return (ans1 / ans2) + 0.636619772 * BesselJ0(x) * System.Math.Log(x);
            }
            else
            {
                double z = 8.0 / x;
                double y = z * z;
                double xx = x - 0.785398164;

                double ans1 = 1.0 + y * (-0.1098628627e-2 + y * (0.2734510407e-4
                    + y * (-0.2073370639e-5 + y * 0.2093887211e-6)));
                double ans2 = -0.1562499995e-1 + y * (0.1430488765e-3
                    + y * (-0.6911147651e-5 + y * (0.7621095161e-6
                    + y * (-0.934945152e-7))));
                return System.Math.Sqrt(0.636619772 / x) *
                    (System.Math.Sin(xx) * ans1 + z * System.Math.Cos(xx) * ans2);
            }
        }

        /// <summary>
        /// Returns the Bessel function of the second kind, of order 1 of the specified number.
        /// </summary>
        public static double BesselY(double x)
        {
            if (x < 8.0)
            {
                double y = x * x;
                double ans1 = x * (-0.4900604943e13 + y * (0.1275274390e13
                    + y * (-0.5153438139e11 + y * (0.7349264551e9
                    + y * (-0.4237922726e7 + y * 0.8511937935e4)))));
                double ans2 = 0.2499580570e14 + y * (0.4244419664e12
                    + y * (0.3733650367e10 + y * (0.2245904002e8
                    + y * (0.1020426050e6 + y * (0.3549632885e3 + y)))));
                return (ans1 / ans2) + 0.636619772 * (BesselJ(x) * System.Math.Log(x) - 1.0 / x);
            }
            else
            {
                double z = 8.0 / x;
                double y = z * z;
                double xx = x - 2.356194491;
                double ans1 = 1.0 + y * (0.183105e-2 + y * (-0.3516396496e-4
                    + y * (0.2457520174e-5 + y * (-0.240337019e-6))));
                double ans2 = 0.04687499995 + y * (-0.2002690873e-3
                    + y * (0.8449199096e-5 + y * (-0.88228987e-6
                    + y * 0.105787412e-6)));
                return System.Math.Sqrt(0.636619772 / x) *
                    (System.Math.Sin(xx) * ans1 + z * System.Math.Cos(xx) * ans2);
            }
        }

        /// <summary>
        /// Returns the Bessel function of the second kind, of order n of the specified number.
        /// </summary>
        public static double BesselY(int n, double x)
        {
            double by, bym, byp, tox;

            if (n == 0) return BesselY0(x);
            if (n == 1) return BesselY(x);

            tox = 2.0 / x;
            by = BesselY(x);
            bym = BesselY0(x);
            for (int j = 1; j < n; j++)
            {
                byp = j * tox * by - bym;
                bym = by;
                by = byp;
            }
            return by;
        }

        /// <summary>
        ///   Computes the Basic Spline of order n
        /// </summary>
        public static double BSpline(int n, double x)
        {
            // ftp://ftp.esat.kuleuven.ac.be/pub/SISTA/hamers/PhD_bhamers.pdf
            // http://sepwww.stanford.edu/public/docs/sep105/sergey2/paper_html/node5.html

            double a = 1.0 / Factorial(n);
            double c;

            bool positive = true;
            for (int k = 0; k <= n + 1; k++)
            {
                c = Binomial(n + 1, k) * TruncPower(x + (n + 1.0) / 2.0 - k, n);
                a += positive ? c : -c;
                positive = !positive;
            }

            return a;
        }

        /// <summary>
        ///   Computes the Binomial Coefficients C(n,k).
        /// </summary>
        public static double Binomial(int n, int k)
        {
            double[] b = new double[n + 1];
            b[0] = 1;

            for (int i = 1; i <= n; ++i)
            {
                b[i] = 1;
                for (int j = i - 1; j > 0; --j)
                    b[j] += b[j - 1];
            }

            return b[k];
        }

        #endregion
        #region other

        public static bool IsSignumEqual(this double a, double b)
        {
            return Math.Sign(a) == Math.Sign(b);
        }

        public static bool IsSignumEqual(this int a, int b)
        {
            return Math.Sign(a) == Math.Sign(b);
        }

        public static double TakeSignumFrom(this double a, double b)
        {
            a = Math.Abs(a);
            if (b < 0) return -a;
            return a;
        }

        public static int TakeSignumFrom(this int a, int b)
        {
            a = Math.Abs(a);
            if (b < 0) return -a;
            return a;
        }

        /// <summary>
        /// returns 1 if i = j, otherwise 0
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public static int KroneckerDelta<T>(int i, int j)
        {
            return (i == j) ? 1 : 0;
        }

        /// <summary>
        /// returns 1 if i = j, otherwise 0
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public static long KroneckerDelta<T>(long i, long j)
        {
            return (i == j) ? 1 : 0;
        }

        /// <summary>
        ///   Returns the next power of 2 after the input value x.
        /// </summary>
        /// <param name="x">Input value x.</param>
        /// <returns>Returns the next power of 2 after the input value x.</returns>
        public static int NextPowerOf2(int x)
        {
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return ++x;
        }

        /// <summary>
        ///   Returns the previous power of 2 after the input value x.
        /// </summary>
        /// <param name="x">Input value x.</param>
        /// <returns>Returns the previous power of 2 after the input value x.</returns>
        public static int PreviousPowerOf2(int x)
        {
            return NextPowerOf2(x + 1) / 2;
        }

        #endregion
        #region todo

        ///// <summary>
        ///// Calcs the n-th Fibonacci-number in O(n)
        ///// </summary>
        ///// <param name="n"></param>
        ///// <returns></returns>
        //public static Complex Fib(int n)
        //{
        //    Matrix M = Ones(2, 2);
        //    M[2, 2] = Complex.Zero;

        //    return (M ^ (n - 1))[1, 1];
        //}

        #endregion

        // TODO: sort out what's useful and what isn't

        //public static Rectangle2D WORLD_BOUNDS_RAD = new Rectangle2D.Double(-Math.PI, -Math.PI/2, Math.PI*2, Math.PI);
        //public static Rectangle2D WORLD_BOUNDS = new Rectangle2D.Double(-180, -90, 360, 180);
	
//*
//    public static void latLongToXYZ(Point2D.Double ll, Point3D xyz) {
//        double c = Math.cos(ll.y);
//        xyz.x = c * Math.cos(ll.x);
//        xyz.y = c * Math.sin(ll.x);
//        xyz.z = Math.sin(ll.y);
//    }

//    public static void xyzToLatLong(Point3D xyz, Point2D.Double ll) {
//        ll.y = MapMath.asin(xyz.z);
//        ll.x = MapMath.atan2(xyz.y, xyz.x);
//    }
//*/

//    public static double greatCircleDistance(double lon1, double lat1, double lon2, double lat2 ) {
//        double dlat = Math.sin((lat2-lat1)/2);
//        double dlon = Math.sin((lon2-lon1)/2);
//        double r = Math.sqrt(dlat*dlat + Math.cos(lat1)*Math.cos(lat2)*dlon*dlon);
//        return 2.0 * Math.asin(r);
//    }

//    public static double sphericalAzimuth(double lat0, double lon0, double lat, double lon) {
//        double diff = lon - lon0;
//        double coslat = Math.cos(lat);

//        return Math.atan2(
//            coslat * Math.sin(diff),
//            (Math.cos(lat0) * Math.sin(lat) -
//            Math.sin(lat0) * coslat * Math.cos(diff))
//        );
//    }

//    public final static int DONT_INTERSECT = 0;
//    public final static int DO_INTERSECT = 1;
//    public final static int COLLINEAR = 2;

//    public static int intersectSegments(Point2D.Double aStart, Point2D.Double aEnd, Point2D.Double bStart, Point2D.Double bEnd, Point2D.Double p) {
//        double a1, a2, b1, b2, c1, c2;
//        double r1, r2, r3, r4;
//        double denom, offset, num;

//        a1 = aEnd.y-aStart.y;
//        b1 = aStart.x-aEnd.x;
//        c1 = aEnd.x*aStart.y - aStart.x*aEnd.y;
//        r3 = a1*bStart.x + b1*bStart.y + c1;
//        r4 = a1*bEnd.x + b1*bEnd.y + c1;

//        if (r3 != 0 && r4 != 0 && sameSigns(r3, r4))
//            return DONT_INTERSECT;

//        a2 = bEnd.y-bStart.y;
//        b2 = bStart.x-bEnd.x;
//        c2 = bEnd.x*bStart.y-bStart.x*bEnd.y;
//        r1 = a2*aStart.x + b2*aStart.y + c2;
//        r2 = a2*aEnd.x + b2*aEnd.y + c2;

//        if (r1 != 0 && r2 != 0 && sameSigns(r1, r2))
//            return DONT_INTERSECT;

//        denom = a1*b2 - a2*b1;
//        if (denom == 0)
//            return COLLINEAR;

//        offset = denom < 0 ? -denom/2 : denom/2;

//        num = b1*c2 - b2*c1;
//        p.x = (num < 0 ? num-offset : num+offset) / denom;

//        num = a2*c1 - a1*c2;
//        p.y = (num < 0 ? num-offset : num+offset) / denom;

//        return DO_INTERSECT;
//    }

//    public static double dot(Point2D.Double a, Point2D.Double b) {
//        return a.x*b.x + a.y*b.y;
//    }
	
//    public static Point2D.Double perpendicular(Point2D.Double a) {
//        return new Point2D.Double(-a.y, a.x);
//    }
	
//    public static Point2D.Double add(Point2D.Double a, Point2D.Double b) {
//        return new Point2D.Double(a.x+b.x, a.y+b.y);
//    }
	
//    public static Point2D.Double subtract(Point2D.Double a, Point2D.Double b) {
//        return new Point2D.Double(a.x-b.x, a.y-b.y);
//    }
	
//    public static Point2D.Double multiply(Point2D.Double a, Point2D.Double b) {
//        return new Point2D.Double(a.x*b.x, a.y*b.y);
//    }
	
//    public static double cross(Point2D.Double a, Point2D.Double b) {
//        return a.x*b.y - b.x*a.y;
//    }

//    public static double cross(double x1, double y1, double x2, double y2) {
//        return x1*y2 - x2*y1;
//    }

//    public static void normalize(Point2D.Double a) {
//        double d = distance(a.x, a.y);
//        a.x /= d;
//        a.y /= d;
//    }
	
//    public static void negate(Point2D.Double a) {
//        a.x = -a.x;
//        a.y = -a.y;
//    }
	
//    public static double longitudeDistance(double l1, double l2) {
//        return Math.min(
//            Math.abs(l1-l2), 
//            ((l1 < 0) ? l1+Math.PI : Math.PI-l1) + ((l2 < 0) ? l2+Math.PI : Math.PI-l2)
//        );
//    }

//    public static double geocentricLatitude(double lat, double flatness) {
//        double f = 1.0 - flatness;
//        return Math.atan((f*f) * Math.tan(lat));
//    }

//    public static double geographicLatitude(double lat, double flatness) {
//        double f = 1.0 - flatness;
//        return Math.atan(Math.tan(lat) / (f*f));
//    }

//    public static double tsfn(double phi, double sinphi, double e) {
//        sinphi *= e;
//        return (Math.tan (.5 * (MapMath.HALFPI - phi)) /
//           Math.pow((1. - sinphi) / (1. + sinphi), .5 * e));
//    }

//    public static double msfn(double sinphi, double cosphi, double es) {
//        return cosphi / Math.sqrt(1.0 - es * sinphi * sinphi);
//    }

//    private final static int N_ITER = 15;

//    public static double phi2(double ts, double e) {
//        double eccnth, phi, con, dphi;
//        int i;

//        eccnth = .5 * e;
//        phi = MapMath.HALFPI - 2. * Math.atan(ts);
//        i = N_ITER;
//        do {
//            con = e * Math.sin(phi);
//            dphi = MapMath.HALFPI - 2. * Math.atan(ts * Math.pow((1. - con) / (1. + con), eccnth)) - phi;
//            phi += dphi;
//        } while (Math.abs(dphi) > 1e-10 && --i != 0);
//        if (i <= 0)
//            throw new ProjectionException();
//        return phi;
//    }

//    private final static double C00 = 1.0;
//    private final static double C02 = .25;
//    private final static double C04 = .046875;
//    private final static double C06 = .01953125;
//    private final static double C08 = .01068115234375;
//    private final static double C22 = .75;
//    private final static double C44 = .46875;
//    private final static double C46 = .01302083333333333333;
//    private final static double C48 = .00712076822916666666;
//    private final static double C66 = .36458333333333333333;
//    private final static double C68 = .00569661458333333333;
//    private final static double C88 = .3076171875;
//    private final static int MAX_ITER = 10;

//    public static double[] enfn(double es) {
//        double t;
//        double[] en = new double[5];
//        en[0] = C00 - es * (C02 + es * (C04 + es * (C06 + es * C08)));
//        en[1] = es * (C22 - es * (C04 + es * (C06 + es * C08)));
//        en[2] = (t = es * es) * (C44 - es * (C46 + es * C48));
//        en[3] = (t *= es) * (C66 - es * C68);
//        en[4] = t * es * C88;
//        return en;
//    }

//    public static double mlfn(double phi, double sphi, double cphi, double[] en) {
//        cphi *= sphi;
//        sphi *= sphi;
//        return en[0] * phi - cphi * (en[1] + sphi*(en[2] + sphi*(en[3] + sphi*en[4])));
//    }

//    public static double inv_mlfn(double arg, double es, double[] en) {
//        double s, t, phi, k = 1./(1.-es);

//        phi = arg;
//        for (int i = MAX_ITER; i != 0; i--) {
//            s = Math.sin(phi);
//            t = 1. - es * s * s;
//            phi -= t = (mlfn(phi, s, Math.cos(phi), en) - arg) * (t * Math.sqrt(t)) * k;
//            if (Math.abs(t) < 1e-11)
//                return phi;
//        }
//        return phi;
//    }

//    private final static double P00 = .33333333333333333333;
//    private final static double P01 = .17222222222222222222;
//    private final static double P02 = .10257936507936507936;
//    private final static double P10 = .06388888888888888888;
//    private final static double P11 = .06640211640211640211;
//    private final static double P20 = .01641501294219154443;

//    public static double[] authset(double es) {
//        double t;
//        double[] APA = new double[3];
//        APA[0] = es * P00;
//        t = es * es;
//        APA[0] += t * P01;
//        APA[1] = t * P10;
//        t *= es;
//        APA[0] += t * P02;
//        APA[1] += t * P11;
//        APA[2] = t * P20;
//        return APA;
//    }

//    public static double authlat(double beta, double []APA) {
//        double t = beta+beta;
//        return(beta + APA[0] * Math.sin(t) + APA[1] * Math.sin(t+t) + APA[2] * Math.sin(t+t+t));
//    }
	
//    public static double qsfn(double sinphi, double e, double one_es) {
//        double con;

//        if (e >= 1.0e-7) {
//            con = e * sinphi;
//            return (one_es * (sinphi / (1. - con * con) -
//               (.5 / e) * Math.log ((1. - con) / (1. + con))));
//        } else
//            return (sinphi + sinphi);
//    }

//    /*
//     * Java translation of "Nice Numbers for Graph Labels"
//     * by Paul Heckbert
//     * from "Graphics Gems", Academic Press, 1990
//     */
//    public static double niceNumber(double x, boolean round) {
//        int expv;				/* exponent of x */
//        double f;				/* fractional part of x */
//        double nf;				/* nice, rounded fraction */

//        expv = (int)Math.floor(Math.log(x) / Math.log(10));
//        f = x/Math.pow(10., expv);		/* between 1 and 10 */
//        if (round) {
//            if (f < 1.5)
//                nf = 1.;
//            else if (f < 3.)
//                nf = 2.;
//            else if (f < 7.)
//                nf = 5.;
//            else
//                nf = 10.;
//        } else if (f <= 1.)
//            nf = 1.;
//        else if (f <= 2.)
//            nf = 2.;
//        else if (f <= 5.)
//            nf = 5.;
//        else
//            nf = 10.;
//        return nf*Math.pow(10., expv);
//    }

        #endregion
    }
}