using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Mathematics.Numerics.Matrices
{
    public class MatrixRow : IEnumerable<Complex>
    {
        #region cvar

        int row;
        Matrix matrix;

        #endregion
        #region prop

        private IEnumerable<Complex> rows
        {
            get
            {
                for (int i = 1; i <= matrix.ColumnCount; i++)
                {
                    yield return matrix[row, i];
                }
            }
        }

        #endregion
        #region ctor

        internal MatrixRow(Matrix matrix, int row)
        {
            this.row = row;
            this.matrix = matrix;
        }

        #endregion
        #region impl

        public IEnumerator<Complex> GetEnumerator()
        {
            return rows.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return rows.GetEnumerator();
        }

        #endregion
    }
}
