using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Mathematics.Numerics.Matrices
{
    internal class PivotMatrix : Matrix
    {
        #region cvar

        private Matrix innerMatrix;
        private Dictionary<int, int> swappedRows;
        private Dictionary<int, int> swappedColumns;

        #endregion
        #region prop

        /// <summary>
        /// access the value at row i, column j
        /// CAUTION: one based!!!
        /// CAUTION: the setter allows adding values outside of the current bounds! 
        /// Setting values outside the current bounds will expand the bounds.
        /// </summary>
        /// <param name="i">one based row index</param>
        /// <param name="j">one based column index</param>
        /// <returns></returns>
        public override Complex this[int i, int j]
        {
            set
            {
                int row = i;
                int col = j;
                
                if (swappedRows.ContainsKey(row)) row = swappedRows[row];
                if (swappedColumns.ContainsKey(col)) col = swappedRows[col];

                innerMatrix[row, col] = value;
            }
            get
            {
                int row = i;
                int col = j;

                if (swappedRows.ContainsKey(row)) row = swappedRows[row];
                if (swappedColumns.ContainsKey(col)) col = swappedRows[col];

                return innerMatrix[row, col];
            }
        }

        #endregion
        #region ctor

        internal PivotMatrix(Matrix innerMatrix) 
            : base(innerMatrix.RowCount, innerMatrix.ColumnCount, innerMatrix.defaultFunction, false, false)
        {
            this.innerMatrix = innerMatrix;
            swappedRows = new Dictionary<int, int>();
            swappedColumns = new Dictionary<int, int>();
        }

        internal PivotMatrix(Matrix innerMatrix, Dictionary<int, int> swappedRows, Dictionary<int, int> swappedColumns)
            : base(innerMatrix.RowCount, innerMatrix.ColumnCount, innerMatrix.defaultFunction, false, false)
        {
            this.innerMatrix = innerMatrix;
            this.swappedRows = swappedRows;
            this.swappedColumns = swappedColumns;
        }

        #endregion
        #region impl

        public override void SwapRows(int srcIndex, int dstIndex)
        {
            swappedRows[srcIndex] = dstIndex;
        }

        public override void SwapColumns(int srcIndex, int dstIndex)
        {
            swappedColumns[srcIndex] = dstIndex;
        }

        /// <summary>
        /// Provides a shallow copy of this matrix.
        /// </summary>
        /// <returns></returns>
        public new PivotMatrix Clone()
        {
            return new PivotMatrix(innerMatrix, swappedRows, swappedColumns);
        }

        #endregion
    }
}
