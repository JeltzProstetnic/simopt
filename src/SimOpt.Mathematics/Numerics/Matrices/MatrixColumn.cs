using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Mathematics.Numerics.Matrices
{
    public class MatrixColumn : IEnumerable<Complex>
    {
        #region cvar

        int column;
        Matrix matrix;

        #endregion
        #region prop

        private IEnumerable<Complex> columns
        {
            get
            {
                for (int i = 1; i <= matrix.RowCount; i++)
                {
                    yield return matrix[i, column];
                }
            }
        }

        #endregion
        #region ctor

        internal MatrixColumn(Matrix matrix, int column)
        {
            this.column = column;
            this.matrix = matrix;
        }

        #endregion
        #region impl

        public IEnumerator<Complex> GetEnumerator()
        {
            return columns.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return columns.GetEnumerator();
        }

        #endregion
    }
}
