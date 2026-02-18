using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Mathematics.Enum;
using System.Collections;

namespace MatthiasToolbox.Mathematics.Numerics.Matrices
{
    public class Matrix : IEquatable<Matrix>
    {
        #region cvar

        protected Dictionary<int, Dictionary<int, Complex>> rows;
        internal Func<int, int, Complex> defaultFunction;
        private Complex defaultValue;

        protected int rowCount = 0;
        protected int columnCount = 0;

        #endregion
        #region over

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            Dictionary<int, int> longestEntryInColumn = new Dictionary<int, int>();
            List<List<string>> entries = new List<List<string>>();
            for (int i = 1; i <= rowCount; i++) entries.Add(new List<string>());

            for (int j = 1; j <= columnCount; j++)
            {
                longestEntryInColumn[j] = 0;
                for (int i = 1; i <= rowCount; i++)
                {
                    entries[i-1].Add(this[i, j].ToStringElaborate());
                    if (entries[i - 1][j - 1].Length > longestEntryInColumn[j]) longestEntryInColumn[j] = this[i, j].ToStringElaborate().Length;
                }
            }

            for (int i = 1; i <= rowCount; i++)
            {
                if (i == 1) result.Append("/ ");
                else if (i == rowCount) result.Append("\\ ");
                else result.Append("| ");

                for (int j = 1; j <= columnCount; j++)
                {
                    result.Append(' ', longestEntryInColumn[j] - entries[i-1][j-1].Length);
                    result.Append(entries[i-1][j-1]);
                }

                if (i == 1) result.Append(" \\");
                else if (i == rowCount) result.Append(" /");
                else result.Append(" |");
                if (i != rowCount) result.Append(Environment.NewLine);
            }

            return result.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj as Matrix == null) return base.Equals(obj);
            return this == (obj as Matrix);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
        #region prop

        #region static const

        private static Complex CZero { get { return Complex.Zero; } }
        private static Complex COne { get { return Complex.One; } }

        #endregion
        #region flags

        /// <summary>
        /// check if there is only one column or only one row
        /// </summary>
        public bool IsVector { get { return columnCount == 1 || rowCount == 1; } }
        
        /// <summary>
        /// check if the number of rows equals one
        /// </summary>
        public bool IsRowVector { get { return rowCount == 1; } }
       
        /// <summary>
        /// Check if the number of columns equals one
        /// </summary>
        public bool IsColumnVector { get { return columnCount == 1; } }

        /// <summary>
        /// Check if the number of rows equals the number of columns.
        /// </summary>
        public bool IsSquare { get { return (this.columnCount == this.rowCount); } }

        #endregion
        #region settings

        #endregion
        #region row & columninfos

        /// <summary>
        /// number of rows
        /// </summary>
        public int RowCount
        {
            get { return rowCount; }
        }

        /// <summary>
        /// number of columns
        /// </summary>
        public int ColumnCount
        {
            get { return columnCount; }
        }

        /// <summary>
        /// get the dimension of the vector if the matrix is a vector. otherwise
        /// an exception will be thrown.
        /// </summary>
        /// <returns></returns>
        public int VectorLength
        {
            get
            {
                if (!IsVector) throw new InvalidOperationException("Attempted to get VectorLength for a multidimensional matrix.");
                return Math.Max(columnCount, rowCount);
            }
        }

        #endregion
        #region indexers

        /// <summary>
        /// access the value at row i, column j
        /// CAUTION: one based!!!
        /// CAUTION: the setter allows adding values outside of the current bounds! 
        /// Setting values outside the current bounds will expand the bounds.
        /// </summary>
        /// <param name="i">one based row index</param>
        /// <param name="j">one based column index</param>
        /// <returns></returns>
        public virtual Complex this[int i, int j]
        {
            set
            {
                if (i <= 0 || j <= 0)
                    throw new ArgumentOutOfRangeException("Indices must not be negative and greater than zero. These are one based here!");

                if (!rows.ContainsKey(i)) rows[i] = new Dictionary<int, Complex>();
                
                if (i > rowCount) rowCount = i;
                if (j > columnCount) columnCount = j;
                
                rows[i][j] = value;
            }
            get
            {
                if (rows.ContainsKey(i) && rows[i].ContainsKey(j))
                {
                    return rows[i][j];
                }
                else if (i > 0 && i <= rowCount && j > 0 && j <= columnCount)
                {
                    return defaultFunction.Invoke(i, j);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Indices exceeded size of matrix.");
                }
            }
        }

        /// <summary>
        /// Access the i-th component of an n by one matrix (column vector)
        /// or one by n matrix (row vector).
        /// </summary>
        /// <param name="i">One-based index.</param>
        /// <returns></returns>
        public virtual Complex this[int i]
        {
            set
            {
                if (IsRowVector)
                {
                    if (i > columnCount) columnCount = i;
                    rows[1][i] = value;
                }
                else if (IsColumnVector)
                {
                    if (i > rowCount) rowCount = i;
                    rows[i][1] = value;
                }
                else
                {
                    throw new InvalidOperationException("Cannot access multidimensional matrix via single index.");
                }
            }
            get
            {                                                    
                if (IsRowVector) return this[1, i]; // Rows[1][i];
                else if (IsColumnVector) return this[i, 1]; //return Rows[i][1];
                else throw new InvalidOperationException("Cannot access multidimensional matrix via single index.");
            }
        }

        ///// <summary>
        ///// access the value at row i, column j
        ///// CAUTION: one based!!!
        ///// CAUTION: the setter allows adding values outside of the current bounds! 
        ///// Setting values outside the current bounds will expand the bounds.
        ///// </summary>
        ///// <param name="i">one based row index</param>
        ///// <param name="j">one based column index</param>
        ///// <returns></returns>
        //public virtual T this[int i, int j]
        //{
        //    set
        //    {
        //        if (i <= 0 || j <= 0)
        //            throw new ArgumentOutOfRangeException("Indices must not be negative and greater than zero. These are one based here!");

        //        if (!Rows.ContainsKey(i)) Rows[i] = new Dictionary<int, Complex>();

        //        if (i > rowCount) rowCount = i;
        //        if (j > columnCount) columnCount = j;

        //        Rows[i][j] = new Complex(value);
        //    }
        //    get
        //    {
        //        if (Rows.ContainsKey(i) && Rows[i].ContainsKey(j))
        //        {
        //            Complex result = Rows[i][j];
        //            if (!result.IsReal) throw new ArgumentException("The value at position (" + i.ToString() + "," + j.ToString() + ") is a complex number.");
        //            return result.Real;
        //        }
        //        else if (i > 0 && i <= rowCount && j > 0 && j <= columnCount)
        //        {
        //            if (!defaultValue.IsReal) throw new ArgumentException("The value at position (" + i.ToString() + "," + j.ToString() + ") is a complex number.");
        //            return defaultValue.Real;
        //        }
        //        else
        //        {
        //            throw new ArgumentOutOfRangeException("Indices exceeded size of matrix.");
        //        }
        //    }
        //}

        #endregion
        #region IEnumerables

        public IEnumerable<MatrixRow> Rows
        {
            get
            {
                for (int i = 1; i <= rowCount; i++)
                {
                    yield return new MatrixRow(this, i);
                }
            }
        }

        public IEnumerable<MatrixColumn> Columns
        {
            get
            {
                for (int i = 1; i <= columnCount; i++)
                {
                    yield return new MatrixColumn(this, i);
                }
            }
        }


        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// empty matrix
        /// </summary>
        private Matrix()
        {
            rows = new Dictionary<int, Dictionary<int, Complex>>();
        }

        #region with default value

        /// <summary>
        /// empty matrix
        /// </summary>
        /// <param name="defaultValue">the value to return for undefined positions. if no default value is given, zero will be assumed</param>
        public Matrix(Complex defaultValue = default(Complex))
            : this()
        {
            this.defaultValue = defaultValue;
            if (defaultValue == default(Complex)) this.defaultValue = CZero;
            defaultFunction = DefaultDefaultFunction;
        }

        /// <summary>
        /// m by n matrix
        /// </summary>
        /// <param name="rowSize">Number of rows</param>
        /// <param name="columnSize">Number of columns</param>
        public Matrix(int rowSize, int columnSize, bool createRows = true, bool createValues = false, Complex defaultValue = default(Complex))
            : this(defaultValue)
        {
            if (!createRows && createValues) 
                throw new ArgumentException("If createRows is set to false, createValues cannot be set to true.");
            
            rowCount = rowSize;
            columnCount = columnSize;

            rows = new Dictionary<int, Dictionary<int, Complex>>();

            if (createRows)
            {
                for (int i = 1; i <= rowSize; i++) // one based!
                {
                    rows[i] = new Dictionary<int, Complex>();
                    if (createValues)
                    {
                        for (int j = 1; j <= columnSize; j++) // one based!
                        {
                            rows[i][j] = defaultValue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// square matrix
        /// </summary>
        /// <param name="n"></param>
        public Matrix(int n, bool createRows = true, bool createValues = false, Complex defaultValue = default(Complex))
            : this(n, n, createRows, createValues, defaultValue)
        { }

        #endregion
        #region with default func

        /// <summary>
        /// empty matrix
        /// </summary>
        /// <param name="defaultValue">the value to return for undefined positions. if no default value function is given, zero will be assumed</param>
        public Matrix(Func<int, int, Complex> defaultValueFunction)
            : this()
        {
            this.defaultFunction = defaultValueFunction;
        }

        /// <summary>
        /// m by n matrix filled with zeros
        /// </summary>
        /// <param name="rowSize">Number of rows</param>
        /// <param name="columnSize">Number of columns</param>
        public Matrix(int rowSize, int columnSize, Func<int, int, Complex> defaultValueFunction, bool createRows = true, bool createValues = false)
            : this(defaultValueFunction)
        {
            if (!createRows && createValues)
                throw new ArgumentException("If createRows is set to false, createValues cannot be set to true.");

            rowCount = rowSize;
            columnCount = columnSize;

            rows = new Dictionary<int, Dictionary<int, Complex>>();

            if (createRows)
            {
                for (int i = 1; i <= rowSize; i++) // one based!
                {
                    rows[i] = new Dictionary<int, Complex>();
                    if (createValues)
                    {
                        for (int j = 1; j <= columnSize; j++) // one based!
                        {
                            rows[i][j] = defaultValue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// square matrix
        /// </summary>
        /// <param name="n"></param>
        public Matrix(int n, Func<int, int, Complex> defaultValueFunction, bool createRows = true, bool createValues = false)
            : this(n, n, defaultValueFunction, createRows, createValues)
        { }

        #endregion
        #region with data

        #endregion
        #region further convenience ctors

        ///// <summary>
        ///// Creates matrix from 2-d Complex array.
        ///// </summary>
        ///// <param name="values"></param>
        //public Matrix(Complex[,] values)
        //{
        //    if (values == null)
        //    {
        //        Values = new ArrayList();
        //        columnCount = 0;
        //        rowCount = 0;
        //    }

        //    rowCount = (int)values.GetLongLength(0);
        //    columnCount = (int)values.GetLongLength(1);

        //    Values = new ArrayList(rowCount);

        //    for (int i = 0; i < rowCount; i++)
        //    {
        //        Values.Add(new ArrayList(columnCount));

        //        for (int j = 0; j < columnCount; j++)
        //        {
        //            ((ArrayList)Values[i]).Add(values[i, j]);
        //        }
        //    }
        //}

        ///// <summary>
        ///// Creates column vector from Complex array.
        ///// </summary>
        ///// <param name="values"></param>
        //public Matrix(Complex[] values)
        //{
        //    if (values == null)
        //    {
        //        Values = new ArrayList();
        //        columnCount = 0;
        //        rowCount = 0;
        //    }

        //    rowCount = values.Length;
        //    columnCount = 1;

        //    Values = new ArrayList(rowCount);

        //    for (int i = 0; i < rowCount; i++)
        //    {
        //        Values.Add(new ArrayList(1));

        //        ((ArrayList)Values[i]).Add(values[i]);
        //    }
        //}

        ///// <summary>
        ///// Creates one by one matrix containing x
        ///// </summary>
        ///// <param name="x"></param>
        //public Matrix(double x)
        //{
        //    rowCount = 1;
        //    columnCount = 1;

        //    Values = new ArrayList(1);

        //    Values.Add(new ArrayList(1));

        //    ((ArrayList)Values[0]).Add(new Complex(x));
        //}

        ///// <summary>
        ///// Creates matrix from 2-d double array.
        ///// </summary>
        ///// <param name="values"></param>
        //public Matrix(double[,] values)
        //{
        //    if (values == null)
        //    {
        //        Values = new ArrayList();
        //        columnCount = 0;
        //        rowCount = 0;
        //    }

        //    rowCount = (int)values.GetLongLength(0);
        //    columnCount = (int)values.GetLongLength(1);

        //    Values = new ArrayList(rowCount);

        //    for (int i = 0; i < rowCount; i++)
        //    {
        //        Values.Add(new ArrayList(columnCount));

        //        for (int j = 0; j < columnCount; j++)
        //        {
        //            ((ArrayList)Values[i]).Add(new Complex(values[i, j]));
        //        }
        //    }
        //}

        ///// <summary>
        ///// Creates column vector from double array.
        ///// </summary>
        ///// <param name="values"></param>
        //public Matrix(double[] values)
        //{
        //    if (values == null)
        //    {
        //        Values = new ArrayList();
        //        columnCount = 0;
        //        rowCount = 0;
        //    }

        //    rowCount = values.Length;
        //    columnCount = 1;

        //    Values = new ArrayList(rowCount);

        //    for (int i = 0; i < rowCount; i++)
        //    {
        //        Values.Add(new ArrayList(1));

        //        ((ArrayList)Values[i]).Add(new Complex(values[i]));
        //    }
        //}

        #endregion

        #endregion

        #region stat

        #region factories

        /// <summary>
        /// create a 1x1 matrix with the given value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Matrix FroComplex(Complex value)
        {
            return new Matrix(1, 1, false, false, value);
        }

        /// <summary>
        /// return a square n*n matrix filled with zeros
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static Matrix Zeros(int n)
        {
            return Zeros(n, n);
        }

        /// <summary>
        /// return a m*n matrix filled with zeros
        /// </summary>
        /// <param name="m"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static Matrix Zeros(int m, int n)
        {
            return new Matrix(m, n, false, false, 0);
        }

        /// <summary>
        /// retrieves the i-th canoncical basis vector in n dimensions
        /// </summary>
        /// <param name="n">dimension of the basis</param>
        /// <param name="j">index of canonical basis vector to be retrieved</param>
        /// <returns></returns>
        public static Matrix E(int n, int i)
        {
            Matrix e = new Matrix(n, false, false);
            e[i] = Complex.One;
            return e;
        }

        /// <summary>
        /// create n by n identity matrix
        /// </summary>
        /// <param name="n">number of rows and columns</param>
        /// <returns>n by n identity matrix</returns>
        public static Matrix Identity(int n)
        {
            return Diag(n, Complex.One);
        }

        /// <summary>
        /// create a n by n matrix filled with ones
        /// </summary>        
        /// <param name="n">number of rows and columns</param>
        /// <returns>n by n matrix filled with ones</returns>        
        public static Matrix Ones(int n)
        {
            return new Matrix(n, false, false, Complex.One);
        }

        /// <summary>
        /// create a n by n matrix filled with ones
        /// </summary>        
        /// <param name="n">number of rows and columns</param>
        /// <returns>n by n matrix filled with ones</returns>        
        public static Matrix Ones(int rowCount, int columnCount)
        {
            return new Matrix(rowCount, columnCount, false, false, Complex.One);
        }

        /// <summary>
        /// generate a diagonal matrix
        /// </summary>
        /// <param name="diagonalValue">value for the diagonal</param>
        /// <returns></returns>
        public static Matrix Diag(int dimension, Complex diagonalValue, Complex defaultValue = default(Complex))
        {
            if (dimension <= 0)
                throw new ArgumentException("Dimension must be >= 0", "dimension");

            Matrix result = new Matrix(dimension, dimension, true, false, defaultValue);

            for (int i = 1; i <= dimension; i++)
            {
                result[i, i] = diagonalValue;
            }

            return result;
        }

        /// <summary>
        /// generate a diagonal matrix
        /// </summary>
        /// <param name="diagonalVector">column vector containing the diagonal elements</param>
        /// <returns></returns>
        public static Matrix Diag(Matrix diagonalVector, Complex defaultValue = default(Complex))
        {
            int dim = diagonalVector.VectorLength;
            if (dim == 0) throw new ArgumentException("Diagonal vector must not be empty!", "diagonalVector");

            Matrix result = new Matrix(dim, dim, true, false, defaultValue);

            for (int i = 1; i <= dim; i++)
            {
                result[i, i] = diagonalVector[i];
            }

            return result;
        }

        /// <summary>
        /// generate a diagonal matrix of the form
        /// 
        /// 1 0 0
        /// 0 1 0
        /// 0 0 1
        /// 
        /// if offset == 0.
        /// 
        /// if offset &gt; 0 the following form will be created:
        /// 
        /// 0 1 0 0
        /// 0 0 1 0
        /// 0 0 0 1
        /// 0 0 0 0
        /// 
        /// and if the offset &lt; 0 the following form will be created:
        /// 
        /// 0 0 0 0
        /// 1 0 0 0
        /// 0 1 0 0
        /// 0 0 1 0
        /// </summary>
        /// <param name="diagonalVector">column vector containing the diag elements</param>
        /// <returns></returns>
        public static Matrix Diag(Matrix diagonalVector, int offset, Complex defaultValue = default(Complex))
        {
            int dim = diagonalVector.VectorLength;
            if (dim == 0) throw new ArgumentException("Diagonal vector must not be empty!", "diagonalVector");

            Matrix result = new Matrix(dim + Math.Abs(offset), dim + Math.Abs(offset), true, false, defaultValue);
            dim = result.RowCount;

            if (offset >= 0)
            {
                for (int i = 1; i <= dim - offset; i++)
                {
                    result[i, i + offset] = diagonalVector[i];
                }
            }
            else
            {
                for (int i = 1; i <= dim + offset; i++)
                {
                    result[i - offset, i] = diagonalVector[i];
                }
            }

            return result;
        }

        /// <summary>
        /// generate a tridiagonal square matrix with constant values on the main and secondary diagonals
        /// </summary>
        /// <param name="lower">value of lower secondary diagonal</param>
        /// <param name="main">value of main diagonal</param>
        /// <param name="upper">value of upper secondary diagonal</param>
        /// <param name="n">dimension of the output matrix</param>
        /// <returns>a tridiagonal matrix</returns>
        public static Matrix TriDiag(Complex lower, Complex main, Complex upper, int n, Complex defaultValue = default(Complex))
        {
            if (n <= 1) throw new ArgumentException("Matrix dimension must greater than one.", "n");
            
            Matrix l = new Matrix(n - 1, 1, false, false, lower);
            Matrix d = new Matrix(n, 1, false, false, main);
            Matrix u = new Matrix(n - 1, 1, false, false, upper);

            return TriDiag(l, d, u, defaultValue);
        }

        /// <summary>
        /// generate a tridiagonal square matrix with given vectors
        /// as main and secondary diagonals.
        /// </summary>
        /// <param name="lower">lower secondary diagonal vector</param>
        /// <param name="main">main diagonal vector</param>
        /// <param name="upper">upper secondary diagonal vector</param>
        /// <returns>a tridiagonal square matrix with given vectors as main and secondary diagonals</returns>
        public static Matrix TriDiag(Matrix lower, Matrix main, Matrix upper, Complex defaultValue = default(Complex))
        {
            int sizeL = lower.VectorLength;
            int sizeD = main.VectorLength;
            int sizeU = upper.VectorLength;

            if (sizeD <= 1) throw new ArgumentException("Main diagonal dimension must greater than one.", "n");
            if (sizeL != sizeU) throw new ArgumentException("Lower and upper secondary diagonal must have the same length.");
            if (sizeL + 1 != sizeD) throw new ArgumentException("Main diagonal must have exactly one element more than the secondary diagonals.");

            // return Diag(l, -1) + Diag(d) + Diag(u, 1); // bad performance!

            Matrix result = new Matrix(sizeD, true, false, defaultValue);

            for (int i = 1; i <= sizeD; i++)
            {
                result[i, i] = main[i];
            }

            for (int i = 1; i <= sizeD - 1; i++)
            {
                result[i, i + 1] = upper[i];
            }

            for (int i = 1; i <= sizeD - 1; i++)
            {
                result[i + 1, i] = lower[i];
            }

            return result;
        }

        ///// <summary>
        ///// Creates m by n chessboard matrix with interchangíng ones and zeros.
        ///// 
        ///// </summary>
        ///// <param name="m">Number of rows.</param>
        ///// <param name="n">Number of columns.</param>
        ///// <param name="even">Indicates, if matrix entry (1,1) equals zero.</param>
        ///// <returns></returns>
        //public static Matrix ChessboardMatrix(int m, int n, bool even)
        //{
        //    Matrix M = new Matrix(m, n);

        //    if (even)
        //        for (int i = 1; i <= m; i++)
        //            for (int j = 1; j <= n; j++)
        //                M[i, j] = KroneckerDelta((i + j) % 2, 0);
        //    else
        //        for (int i = 1; i <= m; i++)
        //            for (int j = 1; j <= n; j++)
        //                M[i, j] = KroneckerDelta((i + j) % 2, 1);

        //    return M;
        //}

        ///// <summary>
        ///// Creates m by n chessboard matrix with interchangíng ones and zeros.
        ///// 
        ///// </summary>        
        ///// <param name="n">Number of columns.</param>
        ///// <param name="even">Indicates, if matrix entry (1,1) equals zero.</param>
        ///// <returns></returns>
        //public static Matrix ChessboardMatrix(int n, bool even)
        //{
        //    Matrix M = new Matrix(n);

        //    if (even)
        //        for (int i = 1; i <= n; i++)
        //            for (int j = 1; j <= n; j++)
        //                M[i, j] = KroneckerDelta((i + j) % 2, 0);
        //    else
        //        for (int i = 1; i <= n; i++)
        //            for (int j = 1; j <= n; j++)
        //                M[i, j] = KroneckerDelta((i + j) % 2, 1);

        //    return M;
        //}

        ///// <summary>
        ///// Creates n by n matrix filled with random values in [0,1];
        ///// all entries on the main diagonal are zero.
        ///// </summary>
        ///// <param name="n"></param>
        ///// <returns></returns>
        //public static Matrix RandomGraph(int n)
        //{
        //    Matrix buf = Random(n, n);

        //    buf -= Diag(buf.DiagVector());

        //    return buf;
        //}

        ///// <summary>
        ///// Creates n by n matrix filled with random values in [0,1];
        ///// all entries on the main diagonal are zero.
        ///// A specified random percentage of edges has weight positive infinity.
        ///// </summary>
        ///// <param name="n"></param>
        ///// <param name="p">Defines probability for an edge being less than +infty. Should be in [0,1],
        ///// p = 1 gives complete directed graph; p = 0 gives no edges.</param>
        ///// <returns></returns>
        //public static Matrix RandomGraph(int n, double p)
        //{
        //    Matrix buf = new Matrix(n);

        //    Random r = new Random();

        //    for (int i = 1; i <= n; i++)
        //        for (int j = 1; j <= n; j++)
        //            if (i == j) buf[i, j] = Complex.Zero;
        //            else if (r.NextDouble() < p) buf[i, j] = new Complex(r.NextDouble());
        //            else buf[i, j] = new Complex(double.PositiveInfinity);

        //    return buf;
        //}

        ///// <summary>
        ///// Creates m by n matrix filled with random values in [0,1].
        ///// </summary>
        ///// <param name="m"></param>
        ///// <param name="n"></param>
        ///// <returns></returns>
        //public static Matrix Random(int m, int n)
        //{
        //    Matrix M = new Matrix(m, n);
        //    Random r = new Random();

        //    for (int i = 1; i <= m; i++)
        //    {
        //        for (int j = 1; j <= n; j++)
        //        {
        //            M[i, j] = new Complex(r.NextDouble());
        //        }
        //    }

        //    return M;
        //}

        ///// <summary>
        ///// Creates n by n matrix filled with random values in [0,1].
        ///// </summary>
        ///// <param name="m"></param>
        ///// <param name="n"></param>
        ///// <returns></returns>
        //public static Matrix Random(int n)
        //{
        //    Matrix M = new Matrix(n);
        //    Random r = new Random();

        //    for (int i = 1; i <= n; i++)
        //    {
        //        for (int j = 1; j <= n; j++)
        //        {
        //            M[i, j] = new Complex(r.NextDouble());
        //        }
        //    }

        //    return M;
        //}

        ///// <summary>
        ///// Creates n by n matrix filled with random values in {lo,...,hi-1}.
        ///// </summary>
        /////<param name="lo">Inclusive lower bound.</param>
        ///// <param name="hi">Exclusive upper bound</param>
        ///// <param name="n">Number of rows and columns each.</param>
        ///// <returns></returns>
        //public static Matrix Random(int n, int lo, int hi)
        //{
        //    Matrix M = new Matrix(n);
        //    Random r = new Random();

        //    for (int i = 1; i <= n; i++)
        //    {
        //        for (int j = 1; j <= n; j++)
        //        {
        //            M[i, j] = new Complex((double)r.Next(lo, hi));
        //        }
        //    }

        //    return M;
        //}

        ///// <summary>
        ///// Creates m by n random zero one matrix with probability p for a one.
        ///// </summary>
        ///// <param name="m">Number of rows.</param>
        ///// <param name="n">Number of columns.</param>
        ///// <param name="p">Probability fro an entry to be one, expecting a value in [0,1].</param>
        ///// <returns></returns>
        //public static Matrix RandomZeroOne(int m, int n, double p)
        //{
        //    Matrix M = new Matrix(m, n);
        //    Random r = new Random();

        //    for (int i = 1; i <= m; i++)
        //        for (int j = 1; j <= n; j++)
        //            if (r.NextDouble() <= p) M[i, j] = Complex.One;

        //    return M;
        //}

        ///// <summary>
        ///// Creates n by n random zero one matrix with probability p for a one.
        ///// </summary>        
        ///// <param name="n">Number of rows and columns, resp.</param>
        ///// <param name="p">Probability fro an entry to be one, expecting a value in [0,1].</param>
        ///// <returns></returns>
        //public static Matrix RandomZeroOne(int n, double p)
        //{
        //    Matrix M = new Matrix(n, n);
        //    Random r = new Random();

        //    for (int i = 1; i <= n; i++)
        //        for (int j = 1; j <= n; j++)
        //            if (r.NextDouble() <= p) M[i, j] = Complex.One;

        //    return M;
        //}

        ///// <summary>
        ///// Creates m by n matrix filled with random values in {lo,...,hi-1}.
        ///// </summary>
        /////<param name="lo">Inclusive lower bound.</param>
        ///// <param name="hi">Exclusive upper bound</param>
        ///// <param name="m">Number of rows.</param>
        ///// <param name="n">Number of columns.</param>
        ///// <returns></returns>
        //public static Matrix Random(int m, int n, int lo, int hi)
        //{
        //    Matrix M = new Matrix(m, n);
        //    Random r = new Random();

        //    for (int i = 1; i <= m; i++)
        //    {
        //        for (int j = 1; j <= n; j++)
        //        {
        //            M[i, j] = new Complex((double)r.Next(lo, hi));
        //        }
        //    }

        //    return M;
        //}

        ///// <summary>
        ///// Creates a random matrix filled with zeros and ones.
        ///// </summary>
        ///// <param name="m">Number of rows.</param>
        ///// <param name="n">Number of columns.</param>
        ///// <param name="p">Probability of each entry being 1.</param>
        ///// <returns></returns>
        //public static Matrix ZeroOneRandom(int m, int n, double p)
        //{
        //    Random r = new Random();

        //    Matrix buf = Zeros(m, n);

        //    for (int i = 1; i <= m; i++)
        //    {
        //        for (int j = 1; j <= n; j++)
        //        {
        //            if (r.NextDouble() <= p)
        //                buf[i, j] = Complex.One;
        //        }
        //    }

        //    return buf;
        //}

        ///// <summary>
        ///// Creates a random matrix filled with zeros and ones.
        ///// </summary>        
        ///// <param name="n">Number of rows and columns.</param>
        ///// <param name="p">Probability of each entry being 1.</param>
        ///// <returns></returns>
        //public static Matrix ZeroOneRandom(int n, double p)
        //{
        //    Random r = new Random();

        //    Matrix buf = Zeros(n);

        //    for (int i = 1; i <= n; i++)
        //    {
        //        for (int j = 1; j <= n; j++)
        //        {
        //            if (r.NextDouble() <= p)
        //                buf[i, j] = Complex.One;
        //        }
        //    }

        //    return buf;
        //}

        ///// <summary>
        ///// Constructs block matrix [A, B; C, D].
        ///// </summary>
        ///// <param name="A">Upper left sub matrix.</param>
        ///// <param name="B">Upper right sub matrix.</param>
        ///// <param name="C">Lower left sub matrix.</param>
        ///// <param name="D">Lower right sub matrix.</param>
        ///// <returns></returns>
        //public static Matrix BlockMatrix(Matrix A, Matrix B, Matrix C, Matrix D)
        //{

        //    if (A.RowCount != B.RowCount || C.RowCount != D.RowCount
        //        || A.ColumnCount != C.ColumnCount || B.ColumnCount != D.ColumnCount)
        //        throw new ArgumentException("Matrix dimensions must agree.");

        //    Matrix R = new Matrix(A.RowCount + C.RowCount, A.ColumnCount + B.ColumnCount);

        //    for (int i = 1; i <= R.rowCount; i++)
        //        for (int j = 1; j <= R.columnCount; j++)
        //            if (i <= A.RowCount)
        //            {
        //                if (j <= A.ColumnCount)
        //                    R[i, j] = A[i, j];
        //                else
        //                    R[i, j] = B[i, j - A.ColumnCount];
        //            }
        //            else
        //            {
        //                if (j <= C.ColumnCount)
        //                    R[i, j] = C[i - A.RowCount, j];
        //                else
        //                    R[i, j] = D[i - A.RowCount, j - C.ColumnCount];
        //            }

        //    return R;
        //}

        //public static Matrix Vandermonde(Complex[] x)
        //{
        //    if (x == null || x.Length < 1)
        //        throw new ArgumentNullException();

        //    int n = x.Length - 1;

        //    Matrix V = new Matrix(n + 1);

        //    for (int i = 0; i <= n; i++)
        //        for (int p = 0; p <= n; p++)
        //            V[i + 1, p + 1] = Complex.Pow(x[i], p);

        //    return V;
        //}

        #endregion

        #region concat

        ///// <summary>
        ///// Vertically concats matrices A and B, which do not have to be of the same height. 
        ///// </summary>
        ///// <param name="A"></param>
        ///// <param name="B"></param>
        ///// <returns>Matrix [A|B]</returns>
        //public static Matrix VerticalConcat(Matrix A, Matrix B)
        //{

        //    Matrix C = A.Column(1);

        //    for (int j = 2; j <= A.ColumnCount; j++)
        //    {
        //        C.InsertColumn(A.Column(j), j);
        //    }


        //    for (int j = 1; j <= B.ColumnCount; j++)
        //    {
        //        C.InsertColumn(B.Column(j), C.ColumnCount + 1);
        //    }

        //    return C;
        //}

        //public static Matrix VerticalConcat(Matrix[] A)
        //{
        //    if (A == null)
        //        throw new ArgumentNullException();
        //    else if (A.Length == 1)
        //        return A[0];
        //    else
        //    {
        //        Matrix C = VerticalConcat(A[0], A[1]);

        //        for (int i = 2; i < A.Length; i++)
        //        {
        //            C = VerticalConcat(C, A[i]);
        //        }

        //        return C;
        //    }
        //}

        //public static Matrix HorizontalConcat(Matrix A, Matrix B)
        //{
        //    Matrix C = A.Row(1);


        //    for (int i = 2; i <= A.RowCount; i++)
        //    {
        //        C.InsertRow(A.Row(i), i);
        //    }


        //    for (int i = 1; i <= B.RowCount; i++)
        //    {
        //        C.InsertRow(B.Row(i), C.RowCount + 1);
        //    }

        //    return C;
        //}

        //public static Matrix HorizontalConcat(Matrix[] A)
        //{
        //    if (A == null)
        //        throw new ArgumentNullException();
        //    else if (A.Length == 1)
        //        return A[0];
        //    else
        //    {
        //        Matrix C = HorizontalConcat(A[0], A[1]);

        //        for (int i = 2; i < A.Length; i++)
        //        {
        //            C = HorizontalConcat(C, A[i]);
        //        }

        //        return C;
        //    }
        //}

        #endregion
       
        // adjacency searches and floyd alg
        ///// <summary>
        ///// Computes all shortest distance between any vertices in a given graph.
        ///// </summary>
        ///// <param name="adjacence_matrix">Square adjacence matrix. The main diagonal
        ///// is expected to consist of zeros, any non-existing edges should be marked
        ///// positive infinity.</param>
        ///// <returns>Two matrices D and P, where D[u,v] holds the distance of the shortest
        ///// path between u and v, and P[u,v] holds the shortcut vertex on the way from
        ///// u to v.</returns>
        //public static Matrix[] Floyd(Matrix adjacence_matrix)
        //{
        //    if (!adjacence_matrix.IsSquare())
        //        throw new ArgumentException("Expected square matrix.");
        //    else if (!adjacence_matrix.IsReal())
        //        throw new ArgumentException("Adjacence matrices are expected to be real.");

        //    int n = adjacence_matrix.RowCount;

        //    Matrix D = adjacence_matrix.Clone(); // distance matrix
        //    Matrix P = new Matrix(n);

        //    double buf;

        //    for (int k = 1; k <= n; k++)
        //        for (int i = 1; i <= n; i++)
        //            for (int j = 1; j <= n; j++)
        //            {
        //                buf = D[i, k].Re + D[k, j].Re;
        //                if (buf < D[i, j].Re)
        //                {
        //                    D[i, j].Re = buf;
        //                    P[i, j].Re = k;
        //                }
        //            }

        //    return new Matrix[] { D, P };
        //}

        ///// <summary>
        ///// Returns the shortest path between two given vertices i and j as
        ///// int array.
        ///// </summary>
        ///// <param name="P">Path matrix as returned from Floyd().</param>
        ///// <param name="i">One-based index of start vertex.</param>
        ///// <param name="j">One-based index of end vertex.</param>
        ///// <returns></returns>
        //public static ArrayList FloydPath(Matrix P, int i, int j)
        //{
        //    if (!P.IsSquare())
        //        throw new ArgumentException("Path matrix must be square.");
        //    else if (!P.IsReal())
        //        throw new ArgumentException("Adjacence matrices are expected to be real.");

        //    ArrayList path = new ArrayList();
        //    path.Add(i);

        //    //int borderliner = 0;
        //    //int n = P.Size()[0] + 1; // shortest path cannot have more than n vertices! 

        //    while (P[i, j] != 0)
        //    {
        //        i = Convert.ToInt32(P[i, j]);
        //        path.Add(i);

        //        //borderliner++;

        //        //if (borderliner == n)
        //        //    throw new FormatException("P was not a Floyd path matrix.");
        //    }

        //    path.Add(j);

        //    return path;
        //}

        
        

        ///// <summary>
        ///// Computes the Householder vector.
        ///// </summary>
        ///// <param name="x"></param>
        ///// <returns></returns>
        //private static Matrix[] HouseholderVector(Matrix x)
        //{
        //    //throw new NotImplementedException("Supposingly buggy!");

        //    //if (!x.IsReal())
        //    //    throw new ArgumentException("Cannot compute housholder vector of non-real vector.");

        //    int n = x.VectorLength();

        //    if (n == 0)
        //        throw new InvalidOperationException("Expected vector as argument.");

        //    Matrix y = x / x.Norm();
        //    Matrix buf = y.Extract(2, n, 1, 1);
        //    Complex s = Dot(buf, buf);

        //    Matrix v = Zeros(n, 1);
        //    v[1] = Complex.One;

        //    v.Insert(2, 1, buf);

        //    double beta = 0;

        //    if (s != 0)
        //    {
        //        Complex mu = Complex.Sqrt(y[1] * y[1] + s);
        //        if (y[1].Re <= 0)
        //            v[1] = y[1] - mu;
        //        else
        //            v[1] = -s / (y[1] + mu);

        //        beta = 2 * v[1].Re * v[1].Re / (s.Re + v[1].Re * v[1].Re);
        //        v = v / v[1];
        //    }

        //    return new Matrix[] { v, new Matrix(beta) };
            
        //}

        #endregion
         
        #region more

        /*
        /// <summary>
        /// Returns the matrix of the real parts of the entries of this matrix.
        /// </summary>
        /// <returns></returns>
        public Matrix Re()
        {
            Matrix M = new Matrix(rowCount, columnCount);

            for (int i = 1; i <= rowCount; i++)
                for (int j = 1; j <= columnCount; j++)
                    M[i, j] = new Complex(this[i, j].Re);

            return M;
        }

        /// <summary>
        /// Returns the matrix of the imaginary parts of the entries of this matrix.
        /// </summary>
        /// <returns></returns>
        public Matrix Im()
        {
            Matrix M = new Matrix(rowCount, columnCount);

            for (int i = 1; i <= rowCount; i++)
                for (int j = 1; j <= columnCount; j++)
                    M[i, j] = new Complex(this[i, j].Im);

            return M;
        }

        /// <summary>
        /// Performs Hessenberg-Householder reduction, where {H, Q}
        /// is returned, with H Hessenbergian, Q orthogonal and H = Q'AQ.
        /// </summary>
        /// <returns></returns>
        public Matrix[] HessenbergHouseholder()
        {
            //throw new NotImplementedException("Still buggy!");

            if (!this.IsSquare())
                throw new InvalidOperationException("Cannot perform Hessenberg Householder decomposition of non-square matrix.");

            int n = rowCount;
            Matrix Q = Identity(n);
            Matrix H = this.Clone();
            Matrix I, N, R, P;
            Matrix[] vbeta = new Matrix[2];
            int m;

            // don't try to understand from the code alone.
            // this is pure magic to me - mathematics, reborn as code.
            for (int k = 1; k <= n - 2; k++)
            {
                vbeta = HouseholderVector(H.Extract(k + 1, n, k, k));
                I = Identity(k);
                N = Zeros(k, n - k);

                m = vbeta[0].VectorLength();
                R = Identity(m) - vbeta[1][1, 1] * vbeta[0] * vbeta[0].Transpose();

                H.Insert(k + 1, k, R * H.Extract(k + 1, n, k, n));
                H.Insert(1, k + 1, H.Extract(1, n, k + 1, n) * R);

                P = BlockMatrix(I, N, N.Transpose(), R);

                Q = Q * P;
            }

            return new Matrix[] { H, Q };

        }

        /// <summary>
        /// Extract sub matrix.
        /// </summary>
        /// <param name="i1">Start row.</param>
        /// <param name="i2">End row.</param>
        /// <param name="j1">Start column.</param>
        /// <param name="j2">End column.</param>
        /// <returns></returns>
        public Matrix Extract(int i1, int i2, int j1, int j2)
        {
            if (i2 < i1 || j2 < j1 || i1 <= 0 || j2 <= 0 || i2 > rowCount || j2 > columnCount)
                throw new ArgumentException("Index exceeds matrix dimension.");

            Matrix B = new Matrix(i2 - i1 + 1, j2 - j1 + 1);

            for (int i = i1; i <= i2; i++)
                for (int j = j1; j <= j2; j++)
                    B[i - i1 + 1, j - j1 + 1] = this[i, j];

            return B;
        }

        

        /// <summary>
        /// Splits matrix into its column vectors.
        /// </summary>
        /// <returns>Array of column vectors.</returns>
        public Matrix[] ColumnVectorize()
        {
            Matrix[] buf = new Matrix[columnCount];

            for (int j = 1; j <= buf.Length; j++)
            {
                buf[j] = this.Column(j);
            }

            return buf;
        }

        /// <summary>
        /// Splits matrix into its row vectors.
        /// </summary>
        /// <returns>Array of row vectors.</returns>
        public Matrix[] RowVectorize()
        {
            Matrix[] buf = new Matrix[rowCount];

            for (int i = 1; i <= buf.Length; i++)
            {
                buf[i] = this.Row(i);
            }

            return buf;
        }

        /// <summary>
        /// Flips matrix vertically.
        /// </summary>
        public void VerticalFlip()
        {
            Values.Reverse();
        }

        /// <summary>
        /// Flips matrix horizontally.
        /// </summary>
        public void HorizontalFlip()
        {
            for (int i = 0; i < rowCount; i++)
            {
                ((ArrayList)Values[i]).Reverse();
            }
        }

        /// <summary>
        /// Deletes row at specifies index.
        /// </summary>
        /// <param name="i">One-based index at which to delete.</param>
        public void DeleteRow(int i)
        {
            if (i <= 0 || i > rowCount)
                throw new ArgumentException("Index must be positive and <= number of rows.");

            Values.RemoveAt(i - 1);
            rowCount--;
        }

        /// <summary>
        /// Deletes column at specifies index.
        /// </summary>
        /// <param name="j">One-based index at which to delete.</param>
        public void DeleteColumn(int j)
        {
            if (j <= 0 || j > columnCount)
                throw new ArgumentException("Index must be positive and <= number of cols.");

            for (int i = 0; i < rowCount; i++)
            {
                ((ArrayList)Values[i]).RemoveAt(j - 1);
            }

            columnCount--;
        }

        /// <summary>
        /// Retrieves row vector at specfifed index and deletes it from matrix.
        /// </summary>
        /// <param name="i">One-based index at which to extract.</param>
        /// <returns>Row vector.</returns>
        public Matrix ExtractRow(int i)
        {
            Matrix buf = this.Row(i);
            this.DeleteRow(i);

            return buf;
        }

        /// <summary>
        /// Retrieves column vector at specfifed index and deletes it from matrix.
        /// </summary>
        /// <param name="j">One-based index at which to extract.</param>
        /// <returns>Row vector.</returns>
        public Matrix ExtractColumn(int j)
        {
            if (j <= 0 || j > columnCount)
                throw new ArgumentException("Index must be positive and <= number of cols.");

            Matrix buf = this.Column(j);
            this.DeleteColumn(j);

            return buf;
        }

        /// <summary>
        /// Inserts row at specified index.
        /// </summary>
        /// <param name="row">Vector to insert</param>
        /// <param name="i">One-based index at which to insert</param>
        public void InsertRow(Matrix row, int i)
        {
            int size = row.VectorLength();

            if (size == 0)
                throw new InvalidOperationException("Row must be a vector of length > 0.");

            if (i <= 0)
                throw new ArgumentException("Row index must be positive.");


            if (i > rowCount)
                this[i, size] = Complex.Zero;

            else if (size > columnCount)
            {
                this[i, size] = Complex.Zero;
                rowCount++;
            }
            else
                rowCount++;



            Values.Insert(--i, new ArrayList(size));
            //Debug.WriteLine(Values.Count.ToString());

            for (int k = 1; k <= size; k++)
            {
                ((ArrayList)Values[i]).Add(row[k]);
            }

            // fill w/ zeros if vector row is too short
            for (int k = size; k < columnCount; k++)
            {
                ((ArrayList)Values[i]).Add(Complex.Zero);
            }
        }

        /// <summary>
        /// Inserts a sub matrix M at row i and column j.
        /// </summary>
        /// <param name="i">One-based row number to insert.</param>
        /// <param name="j">One-based column number to insert.</param>
        /// <param name="M">Sub matrix to insert.</param>
        public void Insert(int i, int j, Matrix M)
        {            
            for (int m = 1; m <= M.rowCount; m++)
                for (int n = 1; n <= M.columnCount; n++)
                    this[i + m - 1, j + n - 1] = M[m, n];
        }

        /// <summary>
        /// Inserts column at specified index.
        /// </summary>
        /// <param name="col">Vector to insert</param>
        /// <param name="j">One-based index at which to insert</param>
        public void InsertColumn(Matrix col, int j)
        {
            int size = col.VectorLength();

            if (size == 0)
                throw new InvalidOperationException("Row must be a vector of length > 0.");

            if (j <= 0)
                throw new ArgumentException("Row index must be positive.");


            if (j > columnCount)
            {
                this[size, j] = Complex.Zero;
            }
            else
                columnCount++;

            if (size > rowCount)
            {
                this[size, j] = Complex.Zero;
            }

            j--;

            for (int k = 0; k < size; k++)
            {
                ((ArrayList)Values[k]).Insert(j, col[k + 1]);
            }

            // fill w/ zeros if vector col too short
            for (int k = size; k < rowCount; k++)
            {
                ((ArrayList)Values[k]).Insert(j, 0);
            }

        }

        /// <summary>
        /// Gram-Schmidtian orthogonalization of an m by n matrix A, such that
        /// {Q, R} is returned, where A = QR, Q is m by n and orthogonal, R is
        /// n by n and upper triangular matrix.
        /// </summary>
        /// <returns></returns>
        public Matrix[] QRGramSchmidt()
        {
            int m = rowCount;
            int n = columnCount;

            Matrix A = this.Clone();

            Matrix Q = new Matrix(m, n);
            Matrix R = new Matrix(n, n);

            // the first column of Q equals the first column of this matrix
            for (int i = 1; i <= m; i++)
                Q[i, 1] = A[i, 1];

            R[1, 1] = Complex.One;

            for (int k = 1; k <= n; k++)
            {
                R[k, k] = new Complex(A.Column(k).Norm());

                for (int i = 1; i <= m; i++)
                    Q[i, k] = A[i, k] / R[k, k];

                for (int j = k + 1; j <= n; j++)
                {
                    R[k, j] = Dot(Q.Column(k), A.Column(j));

                    for (int i = 1; i <= m; i++)
                        A[i, j] = A[i, j] - Q[i, k] * R[k, j];
                }
            }

            return new Matrix[] { Q, R };
        }

        /// <summary>
        /// Computes approximates of the eigenvalues of this matrix. WARNING: Computation
        /// uses basic QR iteration with Gram-Schmidtian orthogonalization. This implies that
        /// (1) only real matrices can be examined; (2) if the matrix has a multiple eigenvalue
        /// or complex eigenvalues, partial junk is returned. This is due to the eigenvalues having
        /// to be like |L1| > |L2| > ... > |Ln| for QR iteration to work properly.
        /// </summary>
        /// <returns></returns>
        public Matrix Eigenvalues()
        {
            return this.QRIterationBasic(40).DiagVector();

        }

        /// <summary>
        /// Computes eigenvector from eigenvalue.
        /// </summary>
        /// <param name="eigenvalue"></param>
        /// <returns></returns>
        public Matrix Eigenvector(Complex eigenvalue)
        {

            throw new NotImplementedException();
            
        }

        /// <summary>
        /// Solves equation this*x = b via conjugate gradient method.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public Matrix SolveCG(Matrix b)
        {
            throw new NotImplementedException("Still buggy!");

            if (!this.IsSPD())
                throw new InvalidOperationException("CG method only works for spd matrices.");
            else if (!this.IsReal())
                throw new InvalidOperationException("CG method only works for real matrices.");

            int n = rowCount;
            int max_iterations = 150;
            double tolerance = 1e-6;

            Matrix x = Ones(n, 1); // x will contain the solution
            Matrix r = b - this * x; // residual approaches zero as x converges to the solution
            Matrix d = r; // dir = direction of descence
            double delta = r.Norm(); // delta denotes the current error
            delta *= delta; 
            tolerance *= tolerance;

            Matrix h = Zeros(n, 1);
            double alpha, gamma;
            double old_delta;

            if (delta <= tolerance)
                return x;
            else
            {
                for (int i = 0; i < max_iterations; i++)
                {
                    h = this * d;
                    gamma = Dot(h, d).Re;

                    if (Math.Abs(gamma) <= tolerance)
                        return x;

                    alpha = delta / gamma;

                    x += alpha * d; // compute new approximation of solution
                    r -= alpha * h; // compute new residual

                    old_delta = delta; // buffer delta

                    delta = r.Norm();
                    delta *= delta;

                    if (delta <= tolerance)
                        return x;

                    d = r + delta / old_delta * d; // compute new direction of descence
                }

                return x;
            }
        }

        /// <summary>
        /// Executes the QR iteration.
        /// </summary>
        /// <param name="max_iterations"></param>
        /// <returns></returns>
        public Matrix QRIterationBasic(int max_iterations)
        {
            if (!this.IsReal())
                throw new InvalidOperationException("Basic QR iteration is possible only for real matrices.");

            Matrix T = this.Clone();
            Matrix[] QR = new Matrix[2];

            for (int i = 0; i < max_iterations; i++)
            {
                QR = T.QRGramSchmidt();
                T = QR[1] * QR[0];
            }

            return T;
        }

        /// <summary>
        /// QR iteration using Hessenberg-Householder reduction.
        /// </summary>
        /// <param name="max_iterations"></param>
        /// <returns></returns>
        public Matrix QRIterationHessenberg(int max_iterations)
        {

            throw new NotImplementedException("Still buggy!");

            if (!this.IsSquare())
                throw new InvalidOperationException("Cannot perform QR iteration of non-square matrix.");

            int n = this.RowCount;

            Matrix[] TQ = this.HessenbergHouseholder();
            Matrix T = TQ[0];

            for (int j = 1; j <= max_iterations; j++)
            {
                Matrix[] QRcs = T.QRGivens();
                T = QRcs[1];

                for (int k = 1; k <= n - 1; k++)
                {
                    T.Gacol(QRcs[2][k], QRcs[3][k], 1, k + 1, k, k + 1);
                }
            }

            return T;
        }

        /// <summary>
        /// QR factorization avec Givens rotations.
        /// </summary>
        /// <param name="H"></param>
        /// <returns></returns>
        public Matrix[] QRGivens()
        {

            throw new NotImplementedException("Still buggy!");

            Matrix H = this.Clone();
            int m = H.RowCount;
            int n = H.ColumnCount;

            Matrix c = Zeros(n - 1, 1);
            Matrix s = Zeros(n - 1, 1);
            Complex[] cs;

            for (int k = 1; k <= n - 1; k++)
            {
                cs = GivensCS(H[k, k], H[k + 1, k]);
                c[k] = cs[0];
                s[k] = cs[1];
                this.Garow(c[k], s[k], 1, k + 1, k, k + 1);
            }

            return new Matrix[] { GivProd(c, s, n), H, c, s };
        }

        /// <summary>
        /// Givens product. Internal use for QRGivens.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="s"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private Matrix GivProd(Matrix c, Matrix s, int n)
        {
            int n1 = n - 1;
            int n2 = n - 2;

            Matrix Q = Eye(n);
            Q[n1, n1] = c[n1];
            Q[n, n] = c[n1];
            Q[n1, n] = s[n1];
            Q[n, n1] = -s[n1];

            for (int k = n2; k >= 1; k--)
            {
                int k1 = k + 1;
                Q[k, k] = c[k];
                Q[k1, k] = -s[k];
                Matrix q = Q.Extract(k1, k1, k1, n);
                Q.Insert(k, k1, s[k] * q);
                Q.Insert(k1, k1, c[k] * q);
            }

            return Q;
        }

        /// <summary>
        /// Product G(i,k,theta)'*this. Internal use for QRGivens.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="s"></param>
        /// <param name="i"></param>
        /// <param name="k"></param>
        /// <param name="j1"></param>
        /// <param name="j2"></param>
        /// <returns></returns>
        private void Garow(Complex c, Complex s, int i, int k, int j1, int j2)
        {
            for (int j = j1; j <= j2; j++)
            {
                Complex t1 = this[i, j];
                Complex t2 = this[k, j];
                this[i, j] = c * t1 - s * t2;
                this[k, j] = s * t1 + c * t2;
            }
        }

        /// <summary>
        /// Product M*G(i,k,theta). Internal use for QRGivens.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="s"></param>
        /// <param name="j1"></param>
        /// <param name="j2"></param>
        /// <param name="i"></param>
        /// <param name="k"></param>
        public void Gacol(Complex c, Complex s, int j1, int j2, int i, int k)
        {
            for (int j = j1; j <= j2; j++)
            {
                Complex t1 = this[j, i];
                Complex t2 = this[j, k];

                this[j, i] = c * t1 - s * t2;
                this[j, k] = s * t1 + c * t2;
            }
        }

        /// <summary>
        /// Computes Givesn sine and cosine.
        /// </summary>
        /// <param name="xi"></param>
        /// <param name="xk"></param>
        /// <returns></returns>
        private Complex[] GivensCS(Complex xi, Complex xk)
        {
            Complex c = Complex.Zero;
            Complex s = Complex.Zero;

            if (xk == 0)
            {
                c = Complex.One;
            }
            else
            {
                if (Complex.Abs(xk) > Complex.Abs(xi))
                {
                    Complex t = -xi / xk;
                    s = 1 / (Complex.Sqrt(1 + t * t));
                    c = s * t;
                }
                else
                {
                    Complex t = -xk / xi;
                    c = 1 / (Complex.Sqrt(1 + t * t));
                    s = c * t;
                }
            }

            return new Complex[] { c, s };
        }

        */

        /*

        /// <summary>
        /// Checks if matrix has a row or column consisting of zeros.
        /// </summary>
        /// <returns>True iff so.</returns>
        public bool HasZeroRowOrColumn()
        {
            for (int i = 1; i <= rowCount; i++)
                if (this.AbsRowSum(i) == 0) return true;

            for (int i = 1; i <= columnCount; i++)
                if (this.AbsColumnSum(i) == 0) return true;

            return false;
        }
       
        public double ColumnSumNorm()
        {
            return this.TaxiNorm();
        }

        public double RowSumNorm()
        {
            return this.MaxNorm();
        }

        /// <summary>
        /// Computes the permanent of the current instance. WARNING: This algorithm has exponential runtime.
        /// Don't use for any but very small instances.
        /// </summary>
        /// <returns></returns>
        public Complex Permanent()
        {
            if (!this.IsSquare())
                throw new InvalidOperationException("Cannot compute permanent of non-square matrix.");

            if (this.HasZeroRowOrColumn())
                return Complex.Zero;

            if (this == Ones(rowCount))
                return new Complex(Factorial(rowCount));

            if (this.IsPermutation())
                return Complex.One;            

            Complex buf = Complex.Zero;

            int minRow = this.GetMinRow();
            int minColumn = this.GetMinColumn();

            if (this.AbsRowSum(minRow) < this.AbsColumnSum(minColumn))
            {
                for (int j = 1; j <= columnCount; j++)
                    if (this[minRow, j] != 0)
                        buf += this[minRow, j] * this.Minor(minRow, j).Permanent();
            }
            else
            {
                for (int i = 1; i <= rowCount; i++)
                    if (this[i, minColumn] != 0)
                        buf += this[i, minColumn] * this.Minor(i, minColumn).Permanent();
            }

            return buf;
        }

        /// <summary>
        /// Finds index of row with minimal AbsRowSum.
        /// </summary>
        /// <returns></returns>
        public int GetMinRow()
        {
            double buf = this.AbsRowSum(1);
            int index = 1;

            double buf2;

            for (int i = 2; i <= rowCount; i++)
            {
                buf2 = this.AbsRowSum(i);
                if (buf2 < buf)
                {
                    buf = buf2;
                    index = i;
                }
            }

            return index;
        }

        /// <summary>
        /// Finds index of column with minimal AbsColumnSum.
        /// </summary>
        /// <returns></returns>
        public int GetMinColumn()
        {
            double buf = this.AbsColumnSum(1);
            int index = 1;

            double buf2;

            for (int j = 2; j <= columnCount; j++)
            {
                buf2 = this.AbsColumnSum(j);
                if (buf2 < buf)
                {
                    buf = buf2;
                    index = j;
                }
            }

            return index;
        }

        private double Factorial(int x)
        {
            double buf = 1;
            for (int i = 2; i <= x; i++)
                buf *= i;

            return buf;
        }

        /// <summary>
        ///  Calcs condition number with respect to inversion
        /// by using |A|*|inv(A)| and 1-Norm.
        /// </summary>
        /// <returns></returns>
        public double Condition()
        {
            return this.TaxiNorm() * this.Inverse().TaxiNorm();
        }

        /// <summary>
        ///  Calcs condition number with respect to inversion
        /// by using |A|*|inv(A)| and p norm.
        /// </summary>
        /// <param name="p">Specifies the norm to be used. Can be one or positive infinity.</param>
        /// <returns></returns>
        public double Condition(int p)
        {
            return this.PNorm(p) * this.Inverse().PNorm(p);
        }

        /// <summary>
        ///  Calcs condition number with respect to inversion
        /// by using |A|*|inv(A)| and frobenius norm.
        /// </summary>
         /// <returns></returns>
        public double ConditionFro()
        {
            return this.FrobeniusNorm() * this.Inverse().FrobeniusNorm();
        }

        /// <summary>
        /// Calcs p-norm of given matrix: p-th root of the sum
        /// of the p-th powers of the absolute values of all matrix entries.
        /// </summary>
        /// <param name="p">Which norm to compute; can be positive infinity.</param>
        /// <returns></returns>
        /// <remarks>If p not in {i, +infty}, *this must be a vector.</remarks>
        public double PNorm(double p)
        {
            if (p <= 0)
                throw new ArgumentException("Argument must be greater than zero.");

            if (p == 1) return TaxiNorm();
            else if (p == double.PositiveInfinity) return MaxNorm();

            int dim = this.VectorLength();
            if (dim == 0)
                throw new InvalidOperationException("Cannot calc p-norm of matrix.");

            double buf = 0;

            for (int i = 1; i <= dim; i++)
                buf += Math.Pow(Complex.Abs(this[i]), p);

            return Math.Pow(buf, 1 / p);
        }

        /// <summary>
        /// 2-Norm for vectors. If *this is a matrix, you might want to choose
        /// FrobeniusNorm().
        /// </summary>
        /// <returns></returns>
        public double Norm()
        {
            return PNorm(2);
        }

        /// <summary>
        /// Frobenius norm of a square matrix. If *this is a vector, this method
        /// is equivalent to Norm() and PNorm(2).
        /// </summary>
        /// <returns></returns>
        public double FrobeniusNorm()
        {
            if (!this.IsSquare())
                throw new InvalidOperationException("Cannot compute frobenius norm of non-square matrix.");

            int n = this.columnCount;
            double buf = 0;

            for (int i = 1; i <= n; i++)
                for (int j = 1; j <= n; j++)
                    buf += (this[i, j] * Complex.Conj(this[i, j])).Re;

            return Math.Sqrt(buf);
        }
  
        /// <summary>
        /// Also known as column-sum norm.
        /// </summary>
        /// <returns>Maximal AbsColumnSum</returns>
        public double TaxiNorm()
        {
            double buf = 0;

            int dim = this.VectorLength();

            if (dim != 0) // vector case
            {
                for (int i = 1; i <= dim; i++)
                {
                    buf += Complex.Abs(this[i]);
                }
            }
            else // general case
            {
                double buf2 = 0;

                for (int j = 1; j <= columnCount; j++)
                {
                    buf2 = AbsColumnSum(j);
                    if (buf2 > buf)
                        buf = buf2;
                }
            }

            return buf;
        }

        /// <summary>
        /// Also known as row-sum norm.
        /// </summary>
        /// <returns>Maximal AbsRowSum</returns>
        public double MaxNorm()
        {
            double buf = 0;
            double buf2 = 0;

            int dim = this.VectorLength();

            if (dim != 0) // vector case
            {
                for (int i = 1; i <= dim; i++)
                {
                    buf2 = Complex.Abs(this[i]);
                    if (buf2 > buf)
                        buf = buf2;
                }
            }
            else // general case
            {
                for (int i = 1; i <= rowCount; i++)
                {
                    buf2 = AbsRowSum(i);
                    if (buf2 > buf)
                        buf = buf2;
                }
            }

            return buf;
        }

        /// <summary>
        /// Calcs sum of the elements of a certain col.
        /// </summary>
        /// <param name="i">One-based index of the col to consider.</param>
        /// <returns></returns>
        public Complex ColumnSum(int j)
        {
            if (j <= 0 || j > columnCount)
                throw new ArgumentException("Index out of range.");

            Complex buf = Complex.Zero;

            j--;

            for (int i = 0; i < rowCount; i++)
            {
                buf += (Complex)(((ArrayList)Values[i])[j]);
            }

            return buf;
        }

        /// <summary>
        /// Calcs sum of the absolute values of the elements of a certain col.
        /// </summary>
        /// <param name="i">One-based index of the col to consider.</param>
        /// <returns></returns>
        public double AbsColumnSum(int j)
        {
            if (j <= 0 || j > columnCount)
                throw new ArgumentException("Index out of range.");

            double buf = 0;
           
            for (int i = 1; i <= rowCount; i++)
            {
                buf += Complex.Abs(this[i,j]);
            }

            return buf;
        }

        /// <summary>
        /// Calcs sum of the elements of a certain row.
        /// </summary>
        /// <param name="i">One-based index of the row to consider.</param>
        /// <returns></returns>
        public Complex RowSum(int i)
        {
            if (i <= 0 || i > rowCount)
                throw new ArgumentException("Index out of range.");

            Complex buf = Complex.Zero;

            ArrayList row = (ArrayList)Values[i - 1];

            for (int j = 0; j < columnCount; j++)
            {
                buf += (Complex)(row[j]);
            }

            return buf;
        }

        /// <summary>
        /// Calcs sum of the absolute values of the elements of a certain row.
        /// </summary>
        /// <param name="i">One-based index of the row to consider.</param>
        /// <returns></returns>
        public double AbsRowSum(int i)
        {
            if (i <= 0 || i > rowCount)
                throw new ArgumentException("Index out of range.");

            double buf = 0;            

            for (int j = 1; j <= columnCount; j++)
            {
                buf += Complex.Abs(this[i, j]);
            }

            return buf;
        }
        
        */

        #endregion

        #region oper

        #region cast

        public static implicit operator Matrix(double[,] values)
        {
            Matrix result = new Matrix(values.GetLength(0), values.GetLength(1));

            for (int i = 1; i <= result.RowCount; i++)
            {
                for (int j = 1; j <= result.ColumnCount; j++)
                {
                    result[i, j] = values[i, j];
                }
            }

            return result;
        }

        public static implicit operator Matrix(double[] values)
        {
            Matrix result = new Matrix(values.GetLength(0), 1);

            for (int i = 1; i <= result.RowCount; i++)
            {
                result[i] = values[i];
            }

            return result;
        }

        #endregion
        #region equality

        /// <summary>
        /// CAUTION: O(n*m)
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static bool operator ==(Matrix A, Matrix B)
        {
            if (A.RowCount != B.RowCount || A.ColumnCount != B.ColumnCount)
                return false;

            for (int i = 1; i <= A.RowCount; i++)
            {
                for (int j = 1; j <= A.ColumnCount; j++)
                {
                    if (A[i, j] != B[i, j]) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// CAUTION: O(n*m)
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static bool operator !=(Matrix A, Matrix B)
        {
            return !(A == B);
        }

        #endregion
        #region arithmetic

        /// <summary>
        /// CAUTION: currently O(m*n)
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static Matrix operator +(Matrix A, Matrix B)
        {
            if (A.RowCount != B.RowCount || A.ColumnCount != B.ColumnCount)
                throw new ArgumentException("Matrices must be of the same dimension.");

            Matrix result = new Matrix(A.rowCount, A.columnCount, true, false, A.defaultValue); // TODO: calc new default value, add only existing data

            for (int i = 1; i <= A.RowCount; i++)
            {
                for (int j = 1; j <= A.ColumnCount; j++)
                {
                    result[i, j] = A[i, j] + B[i, j];
                }
            }

            return result;
        }

        /// <summary>
        /// CAUTION: currently O(m*n)
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static Matrix operator -(Matrix A, Matrix B)
        {
            if (A.RowCount != B.RowCount || A.ColumnCount != B.ColumnCount)
                throw new ArgumentException("Matrices must be of the same dimension.");

            Matrix result = new Matrix(A.rowCount, A.columnCount, true, false, A.defaultValue); // TODO: calc new default value, subtract only existing data

            for (int i = 1; i <= A.RowCount; i++)
            {
                for (int j = 1; j <= A.ColumnCount; j++)
                {
                    result[i, j] = A[i, j] - B[i, j];
                }
            }

            return result;
        }

        public static Matrix operator -(Matrix A)
        {
            Matrix result = new Matrix(A.rowCount, A.columnCount, A.NegatedDefaultFunctionInvoker, false);

            foreach (KeyValuePair<int, Dictionary<int, Complex>> kvp in A.rows)
            {
                foreach (KeyValuePair<int, Complex> kvp2 in kvp.Value)
                {
                    result.rows[kvp.Key][kvp2.Key] = -kvp2.Value;
                }
            }

            return A;
        }

        /// <summary>
        /// Cross Product
        /// CAUTION: O(m*n²)
        /// CAUTION: this operation is not commutative!!! (A * B != B * A)
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Matrix operator *(Matrix left, Matrix right)
        {
            if (left.ColumnCount != right.RowCount)
                throw new ArgumentException("Inner matrix dimensions must be equal for matrix multiplication.");

            Matrix result = new Matrix(left.RowCount, right.ColumnCount);

            for (int i = 1; i <= left.RowCount; i++)
            {
                for (int j = 1; j <= right.ColumnCount; j++)
                {
                    result[i, j] = Dot(left.Row(i), right.Column(j));
                }
            }
            
            return result;
        }

        /// <summary>
        /// CAUTION: O(m*n)
        /// </summary>
        /// <param name="A"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Matrix operator *(Matrix A, Complex x)
        {
            Matrix result = new Matrix(A.rowCount, A.columnCount);

            for (int i = 1; i <= A.RowCount; i++)
            {
                for (int j = 1; j <= A.ColumnCount; j++)
                {
                    result[i, j] = A[i, j] * x;
                }
            }

            return result;
        }

        /// <summary>
        /// CAUTION: O(m*n)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="A"></param>
        /// <returns></returns>
        public static Matrix operator *(Complex x, Matrix A)
        {
            Matrix result = new Matrix(A.RowCount, A.ColumnCount);

            for (int i = 1; i <= A.RowCount; i++)
            {
                for (int j = 1; j <= A.ColumnCount; j++)
                {
                    result[i, j] = A[i, j] * x;
                }
            }

            return result;
        }

        /// <summary>
        /// CAUTION: O(m*n)
        /// </summary>
        /// <param name="A"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Matrix operator /(Matrix A, Complex x)
        {
            return (Complex.One / x) * A;
        }

        #endregion
        #region power operator

        /// <summary>
        /// CAUTION: O(n*m²*k)
        /// </summary>
        /// <param name="A"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static Matrix operator ^(Matrix A, int k)
        {
            if (k < 0)
            {
                if (A.IsSquare) return A.InverseLeverrier() ^ (-k);
                else throw new InvalidOperationException("Cannot invert non-square matrix.");
            }
            else if (k == 0)
            {
                if (A.IsSquare) return Matrix.Identity(A.RowCount);
                else throw new InvalidOperationException("Cannot take non-square matrix to the power of zero.");
            }
            else if (k == 1)
            {
                if (A.IsSquare) return A.Clone();
                else throw new InvalidOperationException("Cannot take non-square matrix to the power of one.");
            }
            else
            {
                Matrix result = A;

                for (int i = 1; i < k; i++)
                {
                    result *= A;
                }

                return result;
            }
        }

        #endregion

        #endregion
        #region impl

        #region math

        /// <summary>
        /// for this matrix A, solve Ax = b via LU factorization 
        /// with column pivoting.
        /// </summary>
        /// <param name="b">a vector of appropriate length</param>
        /// <remarks>approx. O(n^3)</remarks>
        public static Matrix Solve(Matrix A, Matrix b)
        {
            Matrix A2 = A.Clone();
            Matrix b2 = b.Clone();

            if (!A2.IsSquare) throw new InvalidOperationException("Cannot uniquely solve a non-square equation system.");

            int n = A2.RowCount;

            Matrix P = A2.LUPivot();

            // PA = LU => [ Ax = b <=> P'LUx = b <=> L(Ux) = (Pb)] since P is orthogonal
            // set y := Ux, solve Ly = Pb by forward insertion
            // and Ux = y by backward insertion

            b2 = P * b2;

            // solve Ly = Pb
            (A2.LowerTrapeze() - Diag(A2.DiagVector()) + Identity(n)).ForwardInsertion(ref b2);

            // this solves Ux = y
            (A2.UpperTrapeze()).BackwardInsertion(ref b2);

            return b2;
        }

        /// <summary>
        /// perform forward insertion for a regular lower triangular matrix
        /// and right side b, such that the solution is saved within b.
        /// The matrix is not changed.
        /// </summary>
        /// <param name="b">vector of length this.RowCount</param>
        /// <param name="checkConfig">true if the triangular configuration should be checked</param>
        public void ForwardInsertion(ref Matrix b, bool checkConfig = false)
        {
            if (checkConfig && !this.IsLowerTriangular()) 
                throw new InvalidOperationException("Cannot perform forward insertion for a matrix not being lower triangular.");
            if (this.DiagProd() == 0) throw new InvalidOperationException("Problem: The matrix is nearly singular.");
            if (!b.IsVector) throw new ArgumentException("Parameter must be a vector.");
            if (b.VectorLength != rowCount) throw new ArgumentException("Parameter must be a vector of the same length as the height of this matrix.");

            for (int j = 1; j <= rowCount - 1; j++)
            {
                b[j] /= this[j, j];
                for (int i = 1; i <= rowCount - j; i++) b[j + i] -= b[j] * this[j + i, j];
            }

            b[rowCount] /= this[rowCount, rowCount];
        }

        /// <summary>
        /// perform backward insertion for a regular upper triangular matrix
        /// and right side b, such that the solution is saved within b.
        /// The matrix is not changed.
        /// </summary>
        /// <param name="b">a vector of length this.RowCount</param>
        /// <param name="checkConfig">true if the triangular configuration should be checked</param>
        public void BackwardInsertion(ref Matrix b, bool checkConfig = false)
        {
            if (!this.IsUpperTriangular())
                throw new InvalidOperationException("Cannot perform backward insertion for matrix not being upper triangular.");
            if (this.DiagProd() == 0) throw new InvalidOperationException("Problem: Matrix is nearly singular.");
            if (!b.IsVector) new ArgumentException("Parameter must be a vector.");
            if (b.VectorLength != rowCount) throw new ArgumentException("Parameter must be avector of the same length as the height of this matrix.");

            for (int j = rowCount; j >= 2; j--)
            {
                b[j] /= this[j, j];
                for (int i = 1; i <= j - 1; i++) b[i] -= b[j] * this[i, j];
            }

            b[1] /= this[1, 1];
        }

        #endregion
        #region checks

        /// <summary>
        /// CAUTION: O(m*n)
        /// check if the matrix consists only of real entries
        /// </summary>
        /// <returns>true only if all entries are real</returns>
        public bool IsReal()
        {
            for (int i = 1; i <= rowCount; i++)
                for (int j = 1; j <= columnCount; j++)
                    if (!this[i, j].IsReal) return false;

            return true;
        }

        /// <summary>
        /// checks if matrix is lower or upper trapeze.
        /// 
        /// Example for a lower trapeze matrix:
        /// 
        /// 1 0 0 0 0
        /// 2 4 0 0 0
        /// 5 3 6 0 0
        /// </summary>
        /// <returns>true if the matrix is a trapeze matrix</returns>
        public bool IsTrapeze()
        {
            return (this.IsUpperTrapeze() || this.IsLowerTrapeze());
        }

        /// <summary>
        /// check if A[i, j] == 0 for i &gt; j.
        /// 
        /// Example for a upper trapeze matrix:
        /// 
        /// 1 2 3 7
        /// 0 4 5 8
        /// 0 0 6 9
        /// </summary>
        /// <returns>true if matrix is an upper trapeze matrix</returns>
        public bool IsUpperTrapeze()
        {
            for (int j = 1; j <= columnCount; j++)
                for (int i = j + 1; i <= rowCount; i++)
                    if (this[i, j] != CZero) return false;

            return true;
        }

        /// <summary>
        /// check if A[i, j] == 0 for i &lt; j.
        /// 
        /// Example for a lower trapeze matrix:
        /// 
        /// 1 0 0 0 0
        /// 2 4 0 0 0
        /// 5 3 6 0 0
        /// </summary>
        /// <returns>true if matrix is a lower trapeze matrix</returns>
        public bool IsLowerTrapeze()
        {
            for (int i = 1; i <= rowCount; i++)
                for (int j = i + 1; j <= columnCount; j++)
                    if (this[i, j] != CZero) return false;

            return true;
        }

        /// <summary>
        /// CAUTION: O(n*m)
        /// check for orthogonality by testing if A*A' == id.
        /// </summary>
        /// <returns>true if this matrix is orthogonal</returns>
        public bool IsOrthogonal()
        {
            return (this.IsSquare && this * this.Transpose() == Identity(this.rowCount));
        }

        /// <summary>
        /// check if matrix is unitary by testing if A^H*A = id
        /// (A^H is the conjugated transpose of A)
        /// </summary>
        /// <returns>true if this matrix is unitary</returns>
        public bool IsUnitary()
        {
            if (!this.IsSquare) return false;
            return (this.ConjTranspose() * this == Identity(rowCount));
        }

        /// <summary>
        /// check if this matrix is hermitian by testing if A^H = A
        /// (A^H is the conjugated transposed of A)
        /// </summary>
        /// <returns>true if this matrix is hermitian</returns>
        public bool IsHermitian()
        {
            if (!this.IsSquare) return false;
            return this.ConjTranspose() == this;
        }

        /// <summary>
        /// CAUTION: O(n*m)
        /// check if this matrix is normal by testing if A*A^H = A^H*A
        /// (A^H is the conjugated transposed of A)
        /// </summary>
        /// <returns></returns>
        public bool IsNormal()
        {
            return (this * this.ConjTranspose() == this.ConjTranspose() * this);
        }

        /// <summary>
        /// check if this matrix is a diagonal matrix
        /// 
        /// Example for a diagonal matrix:
        /// 
        /// 1 0 0
        /// 0 2 0
        /// 0 0 3
        /// </summary>
        /// <returns>true if this matrix is a diagonal matrix</returns>
        public bool IsDiagonal()
        {
            for (int i = 1; i <= rowCount; i += 1)
            {
                for (int j = 1; j <= columnCount; j += 1)
                {
                    if (i != j && this[i, j] != CZero) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// CAUTION: O(n*m)
        /// check if this matrix is involutary by testing if A*A = id.
        /// </summary>
        /// <returns>true if this matrix is involutary</returns>
        public bool IsInvolutary()
        {
            return (this * this == Identity(rowCount));
        }

        /// <summary>
        /// check if this matrix is symmetric by testing if A[i, j] == A[j, i]
        /// 
        /// Example for a symmetric matrix:
        /// 
        /// 0 1 2 4
        /// 1 0 3 5
        /// 2 3 0 6
        /// 4 5 6 0
        /// </summary>
        /// <returns>True iff matrix is symmetric.</returns>
        public bool IsSymmetric()
        {
            for (int i = 2; i <= this.rowCount; i++)
            {
                for (int j = 1; j < i; j++)
                {
                    if (this[i, j] != this[j, i]) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// check if this matrix is trapeze and square.
        /// 
        /// Example for a upper triangular matrix:
        /// 
        /// 1 2 3
        /// 0 4 5
        /// 0 0 6
        /// </summary>
        /// <returns>true if this matrix is triangular</returns>
        public bool IsTriangular()
        {
            return (this.IsLowerTriangular() || this.IsUpperTriangular());
        }

        /// <summary>
        /// check if this matrix is square and upper trapeze.
        /// 
        /// Example for a upper triangular matrix:
        /// 
        /// 1 2 3 
        /// 0 4 5 
        /// 0 0 6 
        /// </summary>
        /// <returns>true if this matrix is upper triangular</returns>
        public bool IsUpperTriangular()
        {
            return (this.IsSquare && this.IsUpperTrapeze());
        }

        /// <summary>
        /// checks if this matrix is square and lower trapeze
        /// 
        /// Example for a lower triangular matrix:
        /// 
        /// 1 0 0
        /// 2 4 0
        /// 3 5 6
        /// </summary>
        /// <returns>true if this matrix is lower triangular</returns>
        public bool IsLowerTriangular()
        {
            return (this.IsSquare && this.IsLowerTrapeze());
        }

        /// <summary>
        /// check if matrix consists only of zeros and ones.
        /// </summary>
        /// <returns></returns>
        public bool IsZeroOneMatrix()
        {
            for (int i = 1; i <= rowCount; i++)
            {
                for (int j = 1; j <= columnCount; j++)
                {
                    if (this[i, j] != 0 && this[i, j] != 1) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// check if this matrix is a permutation of the identity matrix.
        /// </summary>
        /// <returns>true if the matrix is a permutation matrix</returns>
        public bool IsPermutation()
        {
            return (!this.IsSquare && this.IsZeroOneMatrix() && this.IsInvolutary());
        }

        #endregion
        #region search

        /// <summary>
        /// Performs depth-first search for a graph given by its adjacence matrix.
        /// </summary>
        /// <param name="adjacence_matrix">A[i,j] = 0 or +infty, if there is no edge from i to j; any non-zero value otherwise.</param>
        /// <param name="root">The vertex to begin the search.</param>
        /// <returns>Adjacence matrix of the computed spanning tree.</returns>
        public static Matrix SearchDepthFirst(Matrix adjacence_matrix, int root)
        {
            if (!adjacence_matrix.IsSquare)
                throw new ArgumentException("Adjacence matrices are expected to be square.");
            else if (!adjacence_matrix.IsReal())
                throw new ArgumentException("Adjacence matrices are expected to be real.");

            int n = adjacence_matrix.RowCount;

            if (root < 1 || root > n)
                throw new ArgumentException("Root must be a vertex of the graph, e.i. in {1, ..., n}.");

            Matrix spanTree = new Matrix(n);

            bool[] marked = new bool[n + 1];

            Stack todo = new Stack();
            todo.Push(root);
            marked[root] = true;

            // adajacence lists for each vertex
            ArrayList[] A = new ArrayList[n + 1];

            for (int i = 1; i <= n; i++)
            {
                A[i] = new ArrayList();

                for (int j = 1; j <= n; j++)
                    if (adjacence_matrix[i, j].Re != 0 && adjacence_matrix[i, j].Im != double.PositiveInfinity)
                        A[i].Add(j);
            }

            int v, w;

            while (todo.Count > 0)
            {
                v = (int)todo.Peek();

                if (A[v].Count > 0)
                {
                    w = (int)A[v][0];

                    if (!marked[w])
                    {
                        marked[w] = true; // mark w
                        Complex entry = spanTree[v, w];
                        entry.Real = 1;
                        spanTree[v, w] = entry; // mark vw
                        todo.Push(w); // one more to search
                    }

                    A[v].RemoveAt(0);
                }
                else
                    todo.Pop();
            }

            return spanTree;
        }

        /// <summary>
        /// Performs broad-first search for a graph given by its adjacence matrix.
        /// </summary>
        /// <param name="adjacence_matrix">A[i,j] = 0 or +infty, if there is no edge from i to j; any non-zero value otherwise.</param>
        /// <param name="root">The vertex to begin the search.</param>
        /// <returns>Adjacence matrix of the computed spanning tree.</returns>
        public static Matrix SearchBroadFirst(Matrix adjacence_matrix, int root)
        {
            if (!adjacence_matrix.IsSquare)
                throw new ArgumentException("Adjacence matrices are expected to be square.");
            else if (!adjacence_matrix.IsReal())
                throw new ArgumentException("Adjacence matrices are expected to be real.");

            int n = adjacence_matrix.RowCount;

            if (root < 1 || root > n)
                throw new ArgumentException("Root must be a vertex of the graph, e.i. in {1, ..., n}.");

            Matrix spanTree = new Matrix(n);

            bool[] marked = new bool[n + 1];

            Queue todo = new Queue();
            todo.Enqueue(root);
            marked[root] = true;

            // adajacence lists for each vertex
            ArrayList[] A = new ArrayList[n + 1];

            for (int i = 1; i <= n; i++)
            {
                A[i] = new ArrayList();

                for (int j = 1; j <= n; j++)
                    if (adjacence_matrix[i, j].Re != 0 && adjacence_matrix[i, j].Re != double.PositiveInfinity)
                        A[i].Add(j);
            }

            int v, w;

            while (todo.Count > 0)
            {
                v = (int)todo.Peek();

                if (A[v].Count > 0)
                {
                    w = (int)A[v][0];

                    if (!marked[w])
                    {
                        marked[w] = true; // mark w
                        Complex entry = spanTree[v, w];
                        entry.Real = 1;
                        spanTree[v, w] = entry; // mark vw
                        todo.Enqueue(w); // one more to search
                    }

                    A[v].RemoveAt(0);
                }
                else
                    todo.Dequeue();
            }

            return spanTree;
        }

        #endregion
        #region definitness

        /// <summary>
        /// get the type of definiteness of a symmetric square matrix.
        /// only works for real matrices. use IsHermitian() for complex matrices.
        /// </summary>
        /// <returns></returns>
        public MatrixDefiniteness Definiteness()
        {
            if (!this.IsSquare || !this.IsSymmetric()) return MatrixDefiniteness.Undefined;
            else if (this == Zeros(this.rowCount, this.columnCount)) return MatrixDefiniteness.Indefinite;
            else if (!this.IsReal()) throw new InvalidOperationException("This test only works for real matrices.");

            // construct orthogonal basis for A
            // using Gram-Schmidt orthogonalization
            int n = this.rowCount;

            Matrix[] y = new Matrix[n + 1];
            for (int i = 0; i <= n; i++) y[i] = Matrix.Zeros(n, 1);

            y[1] = this.Column(1);

            Matrix xk;
            Matrix buf;

            // Gram-Schmidt:
            for (int k = 2; k <= n; k++)
            {
                xk = this.Column(k);
                buf = Zeros(n, 1);

                for (int i = 1; i < k; i++)
                {
                    buf += y[i] * Dot(xk, this * y[i]) / Dot(y[i], this * y[i]);
                }

                y[k] = xk - buf;
            }

            // test for definiteness; 
            // e.g. A pos. def. <=> A > 0 <=> y[i]'Ay[i] > 0 for all i (same for neg. def., ...)

            bool strict = true; // pos. def || neg. def.
            Complex result;

            for (int i = 1; i < n; i++)
            {
                result = Dot(y[i], this * y[i]) * Dot(y[i + 1], this * y[i + 1]);

                if (result == 0) strict = false;
                else if (result.Real < 0) return MatrixDefiniteness.Indefinite;
            }

            if (Dot(y[1], this * y[1]).Real >= 0)
            {
                if (strict) return MatrixDefiniteness.PositiveDefinite;
                else return MatrixDefiniteness.PositiveSemidefinite;
            }
            else
            {
                if (strict) return MatrixDefiniteness.NegativeDefinite;
                else return MatrixDefiniteness.NegativeSemidefinite;
            }
        }

        /// <summary>
        /// check if this is symmetric and positive definiteness.
        /// </summary>
        /// <returns>true if this matrix is symmetrix positive definite</returns>
        public bool IsSymmetricPositiveDefinite()
        {
            return this.Definiteness() == MatrixDefiniteness.PositiveDefinite;
        }

        /// <summary>
        /// check if this is symmetric and positive definiteness.
        /// </summary>
        /// <returns>true if this matrix is symmetrix positive definite</returns>
        public bool IsSPD()
        {
            return this.IsSymmetricPositiveDefinite();
        }

        #endregion
        #region decomposition

        /// <summary>
        /// LU-decomposes this matrix if the matrix is not (near) singular. 
        /// For near singular cases use LUPivot instead.
        /// CAUTION: the original data will be overwritten.
        /// Use <code>static bool LU(Matrix original, out Matrix L, out Matrix U)</code> 
        /// if you want to keep the original.
        /// </summary>
        /// <returns>false if a main diagonal entry is zero</returns>
        public bool LU()
        {
            if (!this.IsSquare) throw new InvalidOperationException("Cannot perform LU-decomposition on a non-square matrix.");

            int n = this.columnCount;

            for (int j = 1; j <= n; j++)
            {
                if (this[j, j] == 0)
                {
                    return false;
                }
            }

            for (int j = 1; j <= n; j++)
            {
                for (int k = 1; k < j; k++)
                {
                    for (int i = k + 1; i <= n; i++)
                    {
                        this[i, j] = this[i, j] - this[i, k] * this[k, j];
                    }
                }

                for (int i = j + 1; i <= n; i++)
                {
                    this[i, j] = this[i, j] / this[j, j];
                }
            }

            return true;
        }

        /// <summary>
        /// calculate the LU decomposition of this matrix and store 
        /// the result combinedly in LU
        /// </summary>
        /// <param name="original"></param>
        /// <param name="LU"></param>
        /// <returns>success flag</returns>
        public static bool LU(Matrix original, out Matrix LU)
        {
            if (!original.IsSquare) throw new InvalidOperationException("Cannot perform LU-decomposition on a non-square matrix.");

            Matrix result = original.Clone();
            int n = result.columnCount;

            for (int j = 1; j <= n; j++)
            {
                if (result[j, j] == 0)
                {
                    LU = null;
                    return false;
                }
            }

            for (int j = 1; j <= n; j++)
            {
                for (int k = 1; k < j; k++)
                {
                    for (int i = k + 1; i <= n; i++)
                    {
                        result[i, j] = result[i, j] - result[i, k] * result[k, j];
                    }
                }

                for (int i = j + 1; i <= n; i++)
                {
                    result[i, j] = result[i, j] / result[j, j];
                }
            }

            LU = result;
            return true;
        }

        /// <summary>
        /// calculate the LU decomposition of this matrix and store 
        /// the results in L and U
        /// </summary>
        /// <param name="original"></param>
        /// <param name="L"></param>
        /// <param name="U"></param>
        /// <returns>success flag</returns>
        public static bool LU(Matrix original, out Matrix L, out Matrix U)
        {
            Matrix result;

            if (!LU(original, out result))
            {
                L = null;
                U = null;
                return false;
            }

            L = result.LowerTrapeze();
            U = result.UpperTrapeze();

            for (int i = 1; i <= L.columnCount; i++) L[i, i] = 1;

            return true;
        }

        /// <summary>
        /// calculate the LU-decomposition of this instance with column pivoting 
        /// and store L and U together in this instance.
        /// CAUTION: the original will be overwritten!
        /// </summary>
        /// <returns>the permutation matrix for pivoting; P*this = L*U</returns>
        public Matrix LUPivot()
        {
            if (!this.IsSquare) throw new InvalidOperationException("Cannot perform LU-decomposition on a non-square matrix.");

            int m;
            int n = this.columnCount;
            Matrix P = Identity(n); // permutation matrix

            for (int j = 1; j <= n; j++)
            {
                // find index m with |this[m,j]| >= |this[i,j]| for all i in {j,...,n}
                if (j < n)
                {
                    m = j;

                    for (int i = j + 1; i <= n; i++)
                    {
                        if (Complex.Abs(this[i, j]) > Complex.Abs(this[m, j])) m = i;
                    }

                    if (m > j)
                    {
                        P.SwapRows(j, m);
                        this.SwapRows(j, m);
                    }

                    if (this[j, j] == 0) throw new DivideByZeroException("Unable to decompose: Matrix is too close to singular.");
                }

                for (int k = 1; k < j; k++)
                {
                    for (int i = k + 1; i <= n; i++)
                    {
                        this[i, j] = this[i, j] - this[i, k] * this[k, j];
                    }
                }

                for (int i = j + 1; i <= n; i++)
                {
                    this[i, j] = this[i, j] / this[j, j];
                }
            }

            return P;
        }

        /// <summary>
        /// calculate the LU-decomposition of this instance with column pivoting 
        /// and store L and U together in LU.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="LU"></param>
        /// <param name="P">the permutation matrix for pivoting; P*this = L*U</param>
        /// <returns>success flag</returns>
        public static bool LUPivot(Matrix original, out Matrix LU, out Matrix P)
        {
            if (!original.IsSquare) throw new InvalidOperationException("Cannot perform LU-decomposition on a non-square matrix.");

            int m;
            int n = original.columnCount;
            P = Identity(n); // permutation matrix
            LU = original.Clone();

            for (int j = 1; j <= n; j++)
            {
                // find index m with |this[m,j]| >= |this[i,j]| for all i in {j,...,n}
                if (j < n)
                {
                    m = j;

                    for (int i = j + 1; i <= n; i++)
                    {
                        if (Complex.Abs(LU[i, j]) > Complex.Abs(LU[m, j])) m = i;
                    }

                    if (m > j)
                    {
                        P.SwapRows(j, m);
                        LU.SwapRows(j, m);
                    }

                    if (LU[j, j] == 0) return false;
                }

                for (int k = 1; k < j; k++)
                {
                    for (int i = k + 1; i <= n; i++)
                    {
                        LU[i, j] = LU[i, j] - LU[i, k] * LU[k, j];
                    }
                }

                for (int i = j + 1; i <= n; i++)
                {
                    LU[i, j] = LU[i, j] / LU[j, j];
                }
            }

            return true;
        }

        /// <summary>
        /// calculate the LU-decomposition of this instance with column pivoting 
        /// </summary>
        /// <param name="original"></param>
        /// <param name="L"></param>
        /// <param name="U"></param>
        /// <param name="P">the permutation matrix for pivoting; P*this = L*U</param>
        /// <returns>success flag</returns>
        public static bool LUPivot(Matrix original, out Matrix L, out Matrix U, out Matrix P)
        {
            Matrix result;

            if (!LUPivot(original, out result, out P))
            {
                L = null;
                U = null;
                return false;
            }

            L = result.LowerTrapeze();
            U = result.UpperTrapeze();

            for (int i = 1; i <= L.columnCount; i++) L[i, i] = 1;

            return true;
        }

        /// <summary>
        /// calculate the Cholesky decomposition of this matrix. 
        /// works only for square, symmetric and positive definite matrices.
        /// matrix A = LL', where L is a lower triangular matrix. L is saved in the
        /// lower triangular part of A.
        /// 
        /// anderer pseudo alg:
        /// 
        /// For i = 1 To n
        ///    For j = 1 To i-1
        ///        Summe = a(i, j)
        ///        For k = 1 To j-1
        ///            Summe = Summe - a(i, k) * a(j, k)
        ///        Next k
        ///        a(i, j) = Summe / a(j, j)
        ///    Next j
        ///    Summe = a(i, i)
        ///    For k = 1 To i-1
        ///        Summe = Summe - a(i, k) * a(i, k)
        ///    Next k
        ///    If Summe &lt;= 0 Then 
        ///        EXIT                  // A ist nicht positiv definit
        ///    else 
        ///        a(i, i) = Sqrt(Summe) // Summe ist positiv
        ///    End If
        /// Next i
        /// 
        /// alles oberhalb der diagonale muss noch auf 0 gesetzt werden.
        /// </summary>
        /// <remarks>
        /// The diagonal elements can be retrieved
        /// by a_{11} = h_{11}^2, a_{ii} = h_{ii}^2 + \sum_{k=1}^{i-1}h_{ik}^2 (i = 2..n).
        /// </remarks>        
        public void Cholesky()
        {
            if (!this.IsSquare)
                throw new InvalidOperationException("Cannot perform Cholesky decomposition of a non-square matrix.");

            // TODO: eliminate this test! the algorithm itself can also check this! (see method comment above)
            if (!this.IsSPD())
                throw new InvalidOperationException("Cannot perform Cholesky decomposition of a matrix which is not symmetric positive definite.");

            int n = rowCount;

            for (int k = 1; k < n; k++)
            {
                this[k, k] = Complex.Sqrt(this[k, k]);

                for (int i = 1; i <= n - k; i++) this[k + i, k] = this[k + i, k] / this[k, k];

                for (int j = k + 1; j <= n; j++)
                {
                    for (int i = 0; i <= n - j; i++)
                        this[j + i, j] = this[j + i, j] - this[j + i, k] * this[j, k];
                }
            }

            this[n, n] = Complex.Sqrt(this[n, n]);
        }

        /// <summary>
        /// reverse the cholesky decomposition
        /// </summary>
        public void CholeskyUndo()
        {
            if (!this.IsSquare) throw new InvalidOperationException("Cannot undo cholesky decomposition of a non-square matrix.");

            this[1, 1] = this[1, 1] ^ 2;

            Complex dummy;

            for (int i = 2; i <= rowCount; i++)
            {
                dummy = 0;
                for (int k = 1; k <= i - 1; k++) dummy += (this[i, k] ^ 2);
                this[i, i] = (this[i, i] ^ 2) + dummy;
            }

            this.SymmetrizeDown();
        }

        #endregion
        #region basic operations

        /// <summary>
        /// calculate the trace of this matrix
        /// </summary>
        /// <returns>the sum of the diagonal elements</returns>
        public Complex Trace()
        {
            if (!this.IsSquare) throw new InvalidOperationException("Cannot calculate the trace of a non-square matrix.");

            Complex buf = CZero;

            for (int i = 1; i <= this.rowCount; i++)
            {
                buf += this[i, i];
            }

            return buf;
        }

        /// <summary>
        /// calculate the product of the main diagonal entries
        /// </summary>
        /// <returns>product of diagonal elements</returns>
        public Complex DiagProd()
        {
            Complex buf = COne;
            int dim = Math.Min(this.rowCount, this.columnCount);

            for (int i = 1; i <= dim; i++)
            {
                buf *= this[i, i];
            }

            return buf;
        }

        /// <summary>
        /// TODO: TEST THIS FUNCTION!!! I DO NOT TRUST IT!
        /// calculate the signum of a permutation matrix, which is 1 for an even
        /// number of swaps and -1 for an odd number of swaps. 
        /// CAUTION: if this is not a permutation matrix, the result is garbage.
        /// </summary>
        /// <returns></returns>
        public int Signum()
        {
            double buf = 1;

            int n = rowCount;
            double fi;
            double fj;

            for (int i = 1; i < n; i++)
            {
                for (fi = 1; fi < n && this[i, (int)fi] != 1; fi++) ;

                for (int j = i + 1; j <= n; j++)
                {
                    for (fj = 1; fj <= n && this[j, (int)fj] != 1; fj++) ;
                    buf *= (fi - fj) / (i - j);
                }
            }

            int result = (int)buf;
            if (result != 1 && result != -1) throw new InvalidOperationException("Something strange happened here: the signum of a matrix was neither 1 nor -1.");
            return result;
        } 

        /// <summary>
        /// swap each matrix entry A[i, j] with A[j, i].
        /// </summary>
        /// <returns>a transposed matrix</returns>
        public Matrix Transpose()
        {
            Matrix result = new Matrix(columnCount, rowCount, TransposedDefaultFunctionInvoker);

            foreach (KeyValuePair<int, Dictionary<int, Complex>> kvp in rows)
            {
                result.rows[kvp.Key] = kvp.Value;
            }

            return result;
        }

        /// <summary>
        /// replace each matrix entry z = x + iy with x - iy.
        /// </summary>
        /// <returns>conjugated matrix</returns>
        public Matrix Conjugate()
        {
            Matrix result = new Matrix(rowCount, columnCount, ConjugatedDefaultFunctionInvoker);

            foreach (KeyValuePair<int, Dictionary<int, Complex>> kvp in rows)
            {
                result.rows[kvp.Key] = kvp.Value;
            }

            return result;
        }

        /// <summary>
        /// conjuagtes and transpose this matrix
        /// </summary>
        /// <returns>a new matrix with conjugated and transposed data</returns>
        public Matrix ConjTranspose()
        {
            return Transpose().Conjugate();
        }

        /// <summary>
        /// calculates the determinant of a given square matrix
        /// </summary>
        /// <returns></returns>
        public Complex Determinant()
        {
            if (!this.IsSquare) throw new InvalidOperationException("Cannot calc determinant of non-square matrix.");
            else if (this.columnCount == 1) return this[1, 1];
            else if (this.IsTrapeze()) return this.DiagProd();
            else
            {
                Matrix X = this.Clone();
                if(X.LU())  return X.DiagProd();
                
                Matrix P = X.LUPivot();
                return P.Signum() * X.DiagProd();
            }
        }

        /// <summary>
        /// inverts square matrix with det != 0.
        /// </summary>
        /// <returns>inverse of the given matrix</returns>
        public Matrix Inverse()
        {
            if (!this.IsSquare) throw new InvalidOperationException("Cannot invert non-square matrix.");

            Complex det = this.Determinant();
            if (det == CZero) throw new InvalidOperationException("Cannot invert (nearly) singular matrix.");

            int n = this.columnCount;
            if (n == 1) return Matrix.FroComplex(1 / det);
            if (this.IsReal() && this.IsOrthogonal()) return this.Transpose();
            else if (this.IsUnitary()) return this.ConjTranspose();

            if (this.IsDiagonal())
            {
                Matrix d = this.DiagVector();
                for (int i = 1; i <= n; i++) d[i] = COne / d[i];
                return Diag(d);
            }

            Matrix buf = new Matrix(n, n, false, false);

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    // buf[i, j] = Math.Pow(-1, i + j) * this.Minor(j + 1, i + 1).Determinant(); // O(m*n)
                    buf[i, j] = Math.Pow(-1, i + j) * new MinorMatrix(this, j + 1, i + 1).Determinant(); // O(1)
                }
            }

            return buf / det;
        }

        /// <summary>
        /// TODO: TEST THIS - I DO NOT TRUST THIS METHOD!
        /// alternative matrix inversion using Leverrier's formula
        /// </summary>
        /// <returns>the inverse of this matrix</returns>
        public Matrix InverseLeverrier()
        {
            if (!this.IsSquare) throw new InvalidOperationException("Cannot invert non-square matrix.");

            int n = this.rowCount;
            Matrix id = Identity(n);
            Matrix buf = Identity(n);
            Complex alpha;

            for (int k = 1; k < n; k++)
            {
                alpha = ((double)1 / k) * Trace();
                buf = alpha * id - this;
            }

            alpha = Trace() / n;
            if (alpha != CZero) return buf / alpha;
            else throw new InvalidOperationException("WARNING: Matrix nearly singular or badly scaled.");
        }

        /// <summary>
        /// make square matrix symmetric by copying the upper half to the lower half.
        /// </summary>
        public void SymmetrizeDown()
        {
            if (!this.IsSquare) throw new InvalidOperationException("Cannot symmetrize non-square matrix.");

            for (int j = 1; j <= columnCount; j++)
                for (int i = j + 1; i <= columnCount; i++)
                    this[i, j] = this[j, i];
        }

        /// <summary>
        /// make square matrix symmetric by copying the lower half to the upper half.
        /// </summary>
        public void SymmetrizeUp()
        {
            if (!this.IsSquare) throw new InvalidOperationException("Cannot symmetrize non-square matrix.");

            for (int i = 1; i <= rowCount; i++)
                for (int j = i + 1; j <= columnCount; j++)
                    this[i, j] = this[j, i];
        }

        #endregion
        #region static operations

        /// <summary>
        /// dot product of two vectors
        /// </summary>
        /// <param name="v">Row or column vector.</param>
        /// <param name="w">Row or column vector.</param>
        /// <returns>Dot product.</returns>
        public static Complex Dot(Matrix v, Matrix w)
        {
            if (!v.IsVector || !w.IsVector) throw new ArgumentException("Arguments must be vectors.");

            int m = v.VectorLength;
            int n = w.VectorLength;

            if (m != n) throw new ArgumentException("Vectors must be of the same length.");

            Complex result = CZero;

            for (int i = 1; i <= m; i++)
            {
                result += v[i] * w[i];
            }

            return result;
        }

        #endregion
        #region partial accessors

        /// <summary>
        /// Retrieves column with one-based index j.
        /// </summary>
        /// <param name="j"></param>
        /// <returns>j-th column...</returns>
        public Matrix Column(int j)
        {
            if (j <= 0 || j > columnCount) throw new ArgumentException("Index exceeds matrix dimensions.");

            Matrix result = new Matrix(rowCount, 1, defaultFunction, true, false);

            foreach (KeyValuePair<int, Dictionary<int, Complex>> kvp in rows)
            {
                if (kvp.Value.ContainsKey(j)) result[kvp.Key] = kvp.Value[j];
            }

            return result;
        }

        /// <summary>
        /// Retrieves row with one-based index i.
        /// </summary>
        /// <param name="i"></param>
        /// <returns>i-th row...</returns>
        public Matrix Row(int i)
        {
            if (i <= 0 || i > rowCount) throw new ArgumentException("Index exceeds matrix dimensions.");

            Matrix result = new Matrix(columnCount, 1, defaultFunction, true, false);

            for (int j = 1; j <= this.columnCount; j++)
            {
                if(rows.ContainsKey(i)) result[j] = this[i, j];
            }

            return result;
        }

        /// <summary>
        /// extract the main diagonal vector of this matrix as column vector
        /// </summary>
        /// <returns></returns>
        public Matrix DiagVector()
        {
            if (!this.IsSquare) throw new InvalidOperationException("Cannot extract diagonal of a non-square matrix.");

            Matrix result = new Matrix(this.columnCount, 1);

            for (int i = 1; i <= this.columnCount; i++)
            {
                result[i] = this[i, i];
            }

            return result;
        }

        /// <summary>
        /// create a matrix that results in the clearing of 
        /// a specified row and column from this matrix
        /// 
        /// CAUTION: O(m*n)
        /// CAUTION: the resulting matrix will have fixed values as opposed to defaultFunction values
        /// </summary>        
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public Matrix Minor(int row, int column)
        {
            Matrix result = new Matrix(rowCount - 1, columnCount - 1);

            int r = 0;
            int c = 0;

            for (int i = 1; i <= RowCount; i++)
            {
                if (i != row)
                {
                    for (int j = 1; j <= ColumnCount; j++)
                    {
                        if (j != column)
                        {
                            result[r, c] = this[i, j];
                            c++;
                        }
                    }
                    c = 0;
                    r++;
                }
            }

            return result;
        }

        /// <summary>
        /// return only the lower trapeze matrix of this matrix.
        /// </summary>
        /// <returns></returns>
        public Matrix LowerTrapeze()
        {
            Matrix result = new Matrix(rowCount, columnCount);

            for (int i = 1; i <= rowCount; i++)
            {
                for (int j = 1; j <= i; j++)
                {
                    result[i, j] = this[i, j];
                }
            }

            return result;
        }

        /// <summary>
        /// return only the upper trapeze matrix of this matrix.
        /// </summary>
        /// <returns></returns>
        public Matrix UpperTrapeze()
        {
            Matrix result = new Matrix(rowCount, columnCount);

            for (int i = 1; i <= rowCount; i++)
            {
                for (int j = i; j <= columnCount; j++)
                {
                    result[i, j] = this[i, j];
                }
            }

            return result;
        }

        #endregion
        #region swap manipulations

        /// <summary>
        /// swaps columns
        /// if the given indices are equal nothing will happen
        /// </summary>
        /// <param name="j1">one-based index of first column</param>
        /// <param name="j2">one-based index of second column</param>       
        public virtual void SwapColumns(int j1, int j2)
        {
            if (j1 <= 0 || j1 > columnCount || j2 <= 0 || j2 > columnCount)
                throw new ArgumentException("Indices must be positive and <= number of columns.");
            if (j1 == j2) return;

            List<Complex> tmpCol = new List<Complex>();
            for (int i = 1; i <= rowCount; i++)
            {
                tmpCol.Add(this[i, j2]);
            }

            for (int i = 1; i <= rowCount; i++)
            {
                this[i, j2] = this[i, j1];
            }

            for (int i = 1; i <= rowCount; i++)
            {
                this[i, j1] = tmpCol[i - 1];
            }
        }

        /// <summary>
        /// swaps two rows
        /// if the given indices are equal nothing will happen
        /// </summary>
        /// <param name="i1">one-based index of first row</param>
        /// <param name="i2">one-based index of second row</param>        
        public virtual void SwapRows(int i1, int i2)
        {
            if (i1 < 1 || i1 > rowCount || i2 < 1 || i2 > rowCount)
                throw new ArgumentException("Indices must be positive and <= number of rows.");
            if (i1 == i2) return;

            Dictionary<int, Complex> tmp = rows[i2];
            rows[i2] = rows[i1];
            rows[i1] = tmp;
        }

        #endregion
        #region IEquatable<Matrix>

        public bool Equals(Matrix other)
        {
            return this == other;
        }

        #endregion

        #endregion
        #region util

        /// <summary>
        /// Provides a shallow copy of this matrix.
        /// </summary>
        /// <returns></returns>
        public Matrix Clone()
        {
            Matrix A = new Matrix(rowCount, columnCount, defaultFunction, true, false);

            foreach (KeyValuePair<int, Dictionary<int, Complex>> kvp in rows)
            {
                foreach (KeyValuePair<int, Complex> kvp2 in kvp.Value)
                {
                    A.rows[kvp.Key][kvp2.Key] = kvp2.Value.Clone();
                }
            }

            return A;
        }

        #region delegates

        private Complex DefaultDefaultFunction(int i, int j)
        {
            return defaultValue;
        }

        private Complex TransposedDefaultFunctionInvoker(int i, int j)
        {
            return defaultFunction.Invoke(j, i);
        }

        private Complex ConjugatedDefaultFunctionInvoker(int i, int j)
        {
            return defaultFunction.Invoke(i, j).Conjugate();
        }

        private Complex NegatedDefaultFunctionInvoker(int i, int j)
        {
            return -defaultFunction.Invoke(i, j);
        }

        #endregion

        #endregion
    }
}