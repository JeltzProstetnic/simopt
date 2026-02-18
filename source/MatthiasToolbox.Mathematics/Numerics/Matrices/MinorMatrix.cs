using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Mathematics.Numerics.Matrices
{
    internal class MinorMatrix : Matrix
    {
        #region cvar

        private Matrix innerMatrix;
        private int deletedRow;
        private int deletedColumn;

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
                
                if (row >= deletedRow) row += 1;
                if (col >= deletedColumn) col += 1;

                innerMatrix[row, col] = value;
            }
            get
            {
                int row = i;
                int col = j;

                if (row >= deletedRow) row += 1;
                if (col >= deletedColumn) col += 1;

                return innerMatrix[row, col];
            }
        }

        #endregion
        #region ctor

        internal MinorMatrix(Matrix innerMatrix, int deletedRow, int deletedColumn) 
            : base(innerMatrix.RowCount - 1, innerMatrix.ColumnCount - 1, innerMatrix.defaultFunction, false, false)
        {
            this.innerMatrix = innerMatrix;
            this.deletedRow = deletedRow;
            this.deletedColumn = deletedColumn;
        }

        #endregion
        #region impl

        /// <summary>
        /// Provides a shallow copy of this matrix.
        /// </summary>
        /// <returns></returns>
        public new MinorMatrix Clone()
        {
            return new MinorMatrix(innerMatrix, deletedRow, deletedColumn);
        }

        #endregion
    }
}