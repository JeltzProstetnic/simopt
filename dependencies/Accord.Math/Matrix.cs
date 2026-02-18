// Accord Math Library
// Accord.NET framework
// http://www.crsouza.com
//
// Copyright © César Souza, 2009
// cesarsouza at gmail.com
//

namespace Accord.Math
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Accord.Math.Decompositions;
    using AForge;
    using AForge.Math;

    /// <summary>
    ///   Static class Matrix. Defines a set of extension methods that operate
    ///   mainly on multidimensional arrays and vectors.
    /// </summary>
    public static class Matrix
    {

        #region Comparison and Rounding
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

        /// <summary>Rounds every element of a matrix up to the given decimal places.</summary>
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

        /// <summary>
        ///   This method should not be called. Use Matrix.IsEqual instead.
        /// </summary>
        public static new bool Equals(object obj)
        {
            throw new NotSupportedException("Use Matrix.IsEqual instead.");
        }
        #endregion


        #region Matrix Algebra

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

        #region Multiplication
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

        #region Division
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


        #region Products
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


        #region Matrix Construction
        /// <summary>
        ///   Gets the diagonal vector from a matrix.
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
            double[,] r = Matrix.Create(size, -1.0 / size);
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


        #region Subsection and elements selection

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
        ///   Stores a column vector into the given column position of the matrix.
        /// </summary>
        public static T[,] SetColumn<T>(this T[,] m, int index, T[] column)
        {
            for (int i = 0; i < column.Length; i++)
                m[i, index] = column[i];

            return m;
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
        ///   Stores a row vector into the given row position of the matrix.
        /// </summary>
        public static T[,] SetRow<T>(this T[,] m, int index, T[] row)
        {
            for (int i = 0; i < row.Length; i++)
                m[index, i] = row[i];

            return m;
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


        #region Matrix Characteristics
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
            double[] norm = Matrix.SquareNorm(a);
            return Matrix.Sqrt(norm);
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
        public static DoubleRange Range(this double[] array)
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
            return new DoubleRange(min, max);
        }

        /// <summary>
        ///   Gets the range of the values in a vector.
        /// </summary>
        public static IntRange Range(this int[] array)
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
            return new IntRange(min, max);
        }

        /// <summary>
        ///   Gets the range of the values accross the columns of a matrix.
        /// </summary>
        public static DoubleRange[] Range(double[,] value)
        {
            DoubleRange[] ranges = new DoubleRange[value.GetLength(1)];

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

                ranges[j] = new DoubleRange(min, max);
            }

            return ranges;
        }

        #endregion


        #region Elementwise operations
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
        #endregion


        #region Conversions


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


        #region Inverses and Linear System Solving
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
            return m.Solve(Matrix.Diagonal(m.GetLength(0), 1.0));
        }

        /// <summary>
        ///   Computes the pseudo-inverse of a matrix.
        /// </summary>
        public static double[,] PseudoInverse(this double[,] m)
        {
            SingularValueDecomposition svd = new SingularValueDecomposition(m);
            return svd.Solve(Matrix.Diagonal(m.GetLength(0), m.GetLength(1), 1.0));
        }
        #endregion


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


        #region Complex Numbers
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
        public static DoubleRange Range(this Complex[] array)
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

            return new DoubleRange(
                System.Math.Sqrt(min),
                System.Math.Sqrt(max));
        }
        #endregion

    }
}
