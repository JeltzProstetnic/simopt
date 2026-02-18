using Accord.Math;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Accord.Math.Test
{


    /// <summary>
    ///This is a test class for MatrixTest and is intended
    ///to contain all MatrixTest Unit Tests
    ///</summary>
    [TestClass()]
    public class MatrixTest
    {

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }


        /// <summary>
        ///A test for Multiply
        ///</summary>
        [TestMethod()]
        public void MultiplyTwoMatrices()
        {
            double[,] a = new double[,]
            { 
              {3.000, 1.000, 0.000 },
              {5.000, 2.000, 1.000}
            };

            double[,] b = new double[,]
            { 
              {2.000, 4.000 },
              {4.000, 6.000 },
              {1.000, 9.000 }
            };

            double[,] expected = new double[,]
            { 
              {10.000, 18.000 },
              {19.000, 41.000 }
            };

            double[,] actual = Matrix.Multiply(a, b);

            Assert.IsTrue(Matrix.IsEqual(expected, actual, 0.0001));
        }


        /// <summary>
        ///A test for Submatrix
        ///</summary>
        [TestMethod()]
        public void MatrixSubmatrix()
        {
            double[,] value = new double[,]
            { 
              { 1.000, 1.000, 1.000 },
              { 2.000, 2.000, 2.000 },
              { 3.000, 3.000, 3.000 }
            };

            double[,] expected = new double[,]
            { 
              { 1.000, 1.000, 1.000 },
              { 3.000, 3.000, 3.000 }
            };

            double[,] actual = Matrix.Submatrix(value, new int[] { 0, 2 });

            Assert.IsTrue(Matrix.IsEqual(actual, expected));
        }

        /// <summary>
        ///A test for Inverse
        ///</summary>
        [TestMethod()]
        public void PseudoInverse()
        {
            double[,] value = new double[,]
                { { 1.0, 1.0 },
                  { 2.0, 2.0 }  };


            double[,] expected = new double[,]
                { { 0.1, 0.2 },
                  { 0.1, 0.2 }  };

            double[,] actual = Matrix.PseudoInverse(value);

            Assert.IsTrue(Matrix.IsEqual(expected, actual, 0.001));
        }

        /// <summary>
        ///A test for Expand
        ///</summary>
        [TestMethod()]
        public void ExpandTest()
        {
            double[][] data = 
            {
               new double[] { 0, 0 },
               new double[] { 0, 1 },
               new double[] { 1, 0 },
               new double[] { 1, 1 }
            };

            int[] count = 
            {
                2,
                1,
                3,
                1
            };

            double[][] expected = 
            {
                new double[] { 0, 0 },
                new double[] { 0, 0 }, // 2
                new double[] { 0, 1 }, // 1
                new double[] { 1, 0 },
                new double[] { 1, 0 },
                new double[] { 1, 0 }, // 3
                new double[] { 1, 1 }, // 1
            };

            double[][] actual = Matrix.Expand(data, count);

            Assert.IsTrue(Matrix.IsEqual(expected.ToMatrix(), actual.ToMatrix()));
        }


        /// <summary>
        ///A test for Equals
        ///</summary>
        [TestMethod()]
        public void IsEqualTest1()
        {
            double[,] a = {
                              {0.2},
                          };

            double[,] b = {
                              {double.NaN},
                          };

            double threshold = 0.2;
            bool expected = false;
            bool actual;
            actual = Matrix.IsEqual(a, b, threshold);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for Equals
        ///</summary>
        [TestMethod()]
        public void IsEqualTest2()
        {
            double[,] a = {
                              {0.2,  0.1, 0.0},
                              {0.2, -0.5, double.NaN},
                              {0.2, -0.1, double.NegativeInfinity},
                          };

            double[,] b = {
                              {0.23,  0.1,  0.0},
                              {0.21, -0.5,  double.NaN},
                              {0.19, -0.11, double.NegativeInfinity},
                          };

            double threshold = 0.03;
            bool expected = true;
            bool actual;
            actual = Matrix.IsEqual(a, b, threshold);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for Equals
        ///</summary>
        [TestMethod()]
        public void IsEqualTest3()
        {
            double[] a = { 1, 1, 1 };
            double x = 1;
            bool expected = true;
            bool actual;

            actual = Matrix.IsEqual(a, x);
            Assert.AreEqual(expected, actual);

            actual = a.IsEqual(x);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for Combine
        ///</summary>
        [TestMethod()]
        public void CombineTest()
        {
            int[][,] matrices =
            {
                new int[,]
                {
                      {0, 1}
                },
                
                new int[,]
                {
                      {1, 0},
                      {1, 0}
                },
                
                new int[,]
                {
                      {0, 2}
                }
            };


            int[,] expected = 
            {
                 {0, 1},
                 {1, 0},
                 {1, 0},
                 {0, 2}
            };

            int[,] actual;
            actual = Matrix.Combine(matrices);

            Assert.IsTrue(Matrix.IsEqual(expected, actual));
        }



        [TestMethod()]
        public void ColumnVectorTest()
        {
            double[] values = { 1, 2, 3 };
            double[,] expected = { 
                                   { 1 },
                                   { 2 },
                                   { 3 }
                                 };
            double[,] actual;
            actual = Matrix.ColumnVector(values);
            Assert.IsTrue(Matrix.IsEqual(expected, actual));
        }

        [TestMethod()]
        public void RowVectorTest()
        {
            double[] values = { 1, 2, 3 };
            double[,] expected = { 
                                    { 1, 2, 3 },
                                 };
            double[,] actual;
            actual = Matrix.RowVector(values);
            Assert.IsTrue(Matrix.IsEqual(expected, actual));
        }

        /// <summary>
        ///A test for Add
        ///</summary>
        [TestMethod()]
        public void AddTwoMatricesTest()
        {
            double[,] a = Matrix.Create(3, 5, 0.0);
            double[] v = { 1, 2, 3, 4, 5 };
            double[,] expected = {
                                     { 1, 2, 3, 4, 5 },
                                     { 1, 2, 3, 4, 5 },
                                     { 1, 2, 3, 4, 5 },
                                 };

            double[,] actual;

            actual = Matrix.Add(a, v, 0); // Add to rows
            Assert.IsTrue(Matrix.IsEqual(expected, actual));


            double[,] b = Matrix.Create(5, 4, 0.0);
            double[] u = { 1, 2, 3, 4, 5 };
            double[,] expected2 = {
                                     { 1, 1, 1, 1, },
                                     { 2, 2, 2, 2, },
                                     { 3, 3, 3, 3, },
                                     { 4, 4, 4, 4, },
                                     { 5, 5, 5, 5, },
                                 };


            actual = Matrix.Add(b, u, 1); // Add to columns
            Assert.IsTrue(Matrix.IsEqual(expected2, actual));

        }

        /// <summary>
        ///A test for Power
        ///</summary>
        [TestMethod()]
        public void PowerTest()
        {
            double[,] a = Matrix.Identity(5);
            int d = 5;
            double[,] expected = Matrix.Identity(5);
            double[,] actual;
            actual = Matrix.Power(a, d);
            Assert.IsTrue(Matrix.IsEqual(expected, actual));
        }

        /// <summary>
        ///A test for Centering
        ///</summary>
        [TestMethod()]
        public void CenteringTest()
        {

            double[,] C2 = Matrix.Centering(2);

            Assert.IsTrue(Matrix.IsEqual(C2, new double[,] { 
                                                             {  0.5, -0.5 },
                                                             { -0.5,  0.5 }
                                                           }));

            double[,] X = {
                              { 1, 5, 2, 0 },
                              { 6, 2, 3, 100 },
                              { 2, 5, 8, 2 },
                          };



            double[,] XC = X.Multiply(Matrix.Centering(4)); // Remove means from rows
            double[,] CX = Matrix.Centering(3).Multiply(X); // Remove means from columns

            double[] colMean = Statistics.Tools.Mean(X, 1);
            double[] rowMean = Statistics.Tools.Mean(X, 0);
            double[,] Xr = X.Subtract(rowMean, 0);          // Remove means from rows
            double[,] Xc = X.Subtract(colMean, 1);          // Remove means from columns

            Assert.IsTrue(Matrix.IsEqual(XC, Xr));
            Assert.IsTrue(Matrix.IsEqual(CX, Xc, 0.00001));

            double[,] S1 = XC.Multiply(X.Transpose());
            double[,] S2 = Xr.Multiply(Xr.Transpose());
            double[,] S3 = Statistics.Tools.Scatter(X, rowMean, 0);

            Assert.IsTrue(Matrix.IsEqual(S1, S2));
            Assert.IsTrue(Matrix.IsEqual(S2, S3));
        }

        /// <summary>
        ///A test for Multiply
        ///</summary>
        [TestMethod()]
        public void MultiplyMatrixVectorTest()
        {
            double[,] a = new double[,] {
                { 4, 5, 1 },
                { 5, 5, 5 },
            };
            double[] b = new double[] { 2, 3, 1 };
            double[] expected = new double[] { 24, 30 };
            double[] actual;
            actual = Matrix.Multiply(a, b);

            Assert.IsTrue(Matrix.IsEqual(expected, actual, 0.0000001));

        }

        /// <summary>
        ///A test for Max
        ///</summary>
        [TestMethod()]
        public void MaxTest()
        {
            double[,] matrix = new double[,]
            {
                { 0, 1, 3, 1},
                { 9, 1, 3, 1},
                { 2, 4, 4, 11},
            };
            int dimension = 1;
            int[] imax = null;
            int[] imaxExpected = new int[] { 2, 0, 3 };
            double[] expected = new double[] { 3, 9, 11 };
            double[] actual;
            actual = Matrix.Max(matrix, dimension, out imax);

            Assert.IsTrue(Matrix.IsEqual(imaxExpected, imax));
            Assert.IsTrue(Matrix.IsEqual(expected, actual));


            dimension = 0;
            imaxExpected = new int[] { 1, 2, 2, 2 };
            expected = new double[] { 9, 4, 4, 11 };

            actual = Matrix.Max(matrix, dimension, out imax);

            Assert.IsTrue(Matrix.IsEqual(imaxExpected, imax));
            Assert.IsTrue(Matrix.IsEqual(expected, actual));

        }


        [TestMethod()]
        public void CombineTest1()
        {
            double[][] vectors = new double[][]
            {
                new double[] { 0, 1, 2 },
                new double[] { 3, 4, },
                new double[] { 5, 6, 7, 8, 9},
            };

            double[] expected = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var actual = Matrix.Combine(vectors);

            Assert.IsTrue(Matrix.IsEqual(expected, actual));

        }


        [TestMethod()]
        public void ReshapeTest()
        {
            int[] array = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            int rows = 3;
            int cols = 3;

            int[,] expected = 
            {
                { 1, 4, 7 },
                { 2, 5, 8 },
                { 3, 6, 9 },
            };

            int[,] actual = Matrix.Reshape(array, rows, cols);

            Assert.IsTrue(Matrix.IsEqual(expected, actual));

        }

        /// <summary>
        ///A test for Multiply
        ///</summary>
        [TestMethod()]
        public void MultiplyVectorMatrixTest()
        {
            double[] a = { 1.000, 2.000, 3.000 };
            double[,] b = 
            { 
                            { 2.000, 1.000, 5.000, 2.000 },
                            { 2.000, 1.000, 2.000, 2.000 },
                            { 1.000, 1.000, 1.000, 1.000 },
             };
            double[] expected = { 9.000, 6.000, 12.000, 9.000 };

            double[] actual;
            actual = Matrix.Multiply(a, b);
            Assert.IsTrue(actual.IsEqual(expected));

        }

        /// <summary>
        ///A test for OuterProduct
        ///</summary>
        [TestMethod()]
        public void OuterProductTest()
        {
            double[] a = { 1, 2, 3, 4 };
            double[] b = { 1, 2, 3, 4 };
            double[,] expected = 
            {
                { 1.000,  2.000,  3.000,  4.000 },
                { 2.000,  4.000,  6.000,  8.000 },
                { 3.000,  6.000,  9.000, 12.000 },
                { 4.000,  8.000, 12.000, 16.000 }
            };

            double[,] actual;
            actual = Matrix.OuterProduct(a, b);
            Assert.IsTrue(expected.IsEqual(actual));
        }
    }
}
