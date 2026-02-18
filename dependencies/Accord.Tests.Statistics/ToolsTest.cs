using Accord.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Accord.Math;

namespace Accord.Statistics.Test
{


    /// <summary>
    ///This is a test class for ToolsTest and is intended
    ///to contain all ToolsTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ToolsTest
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

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for Scatter
        ///</summary>
        [TestMethod()]
        public void ScatterTest()
        {

        }

        /// <summary>
        ///A test for Covariance
        ///</summary>
        [TestMethod()]
        public void CovarianceTest()
        {
            double[,] matrix = new double[,]
            {
                { 4.0, 2.0, 0.60 },
                { 4.2, 2.1, 0.59 },
                { 3.9, 2.0, 0.58 },
                { 4.3, 2.1, 0.62 },
                { 4.1, 2.2, 0.63 }
            };


            double[,] expected = new double[,]
            {
                { 0.02500, 0.00750, 0.00175 },
                { 0.00750, 0.00700, 0.00135 },
                { 0.00175, 0.00135, 0.00043 },
            };


            double[,] actual = Tools.Covariance(matrix);

            Assert.IsTrue(Matrix.IsEqual(expected, actual, 0.0001));

        }

        /// <summary>
        ///A test for Correlation
        ///</summary>
        [TestMethod()]
        public void CorrelationTest()
        {
            // http://www.solvemymath.com/online_math_calculator/statistics/descriptive/correlation.php

            double[,] matrix = new double[,]
            {
                { 4.0, 2.0, 0.60 },
                { 4.2, 2.1, 0.59 },
                { 3.9, 2.0, 0.58 },
                { 4.3, 2.1, 0.62 },
                { 4.1, 2.2, 0.63 }
            };


            double[,] expected = new double[,]
            {
                { 1.000000, 0.5669467, 0.533745 },
                { 0.566946, 1.0000000, 0.778127 },
                { 0.533745, 0.7781271, 1.000000 }
            };


            double[,] actual = Tools.Correlation(matrix);

            Assert.IsTrue(Matrix.IsEqual(expected, actual, 0.001));

        }



        /// <summary>
        ///A test for Proportions
        ///</summary>
        [TestMethod()]
        public void ProportionsTest()
        {
            int[,] summary = {
                           { 1, 4, 5 },
                           { 2, 1, 3 },
                          };

            double[,] probabilities = {
                               { 1, 4.0/(4+5) },
                               { 2, 1.0/(1+3) },
                           };

            int[] positives = summary.GetColumn(1);
            int[] negatives = summary.GetColumn(2);
            double[] expected = probabilities.GetColumn(1);
            double[] actual;

            actual = Tools.Proportions(positives, negatives);

            Assert.IsTrue(Matrix.IsEqual(expected, actual, 0.000000001));
        }

        /// <summary>
        ///A test for Group
        ///</summary>
        [TestMethod()]
        public void GroupTest()
        {

            int[][] data = {
                 new int[] { 1, 1 },
                 new int[] { 1, 1 },
                 new int[] { 1, 1 },
                 new int[] { 1, 1 },
                 new int[] { 1, 0 },
                 new int[] { 1, 0 },
                 new int[] { 1, 0 },
                 new int[] { 1, 0 },
                 new int[] { 1, 0 },
                 new int[] { 2, 1 },
                 new int[] { 2, 0 },
                 new int[] { 2, 0 },
                 new int[] { 2, 0 },
            };

            int[][] expected = {
                 new int[] { 1, 4, 5 },
                 new int[] { 2, 1, 3 },
            };


            int[][] actual;
            actual = Tools.Group(data, 0, 1);

            Assert.IsTrue(Matrix.IsEqual(expected, actual));
        }

        /// <summary>
        ///A test for Extend
        ///</summary>
        [TestMethod()]
        public void ExtendTest()
        {
            int[,] summary = {
                           { 1, 4, 5 },
                           { 2, 1, 3 },
                          };

            int[] group = summary.GetColumn(0);
            int[] positives = summary.GetColumn(1);
            int[] negatives = summary.GetColumn(2);
            int[][] expected = {
                 new int[] { 1, 1 },
                 new int[] { 1, 1 },
                 new int[] { 1, 1 },
                 new int[] { 1, 1 },
                 new int[] { 1, 0 },
                 new int[] { 1, 0 },
                 new int[] { 1, 0 },
                 new int[] { 1, 0 },
                 new int[] { 1, 0 },
                 new int[] { 2, 1 },
                 new int[] { 2, 0 },
                 new int[] { 2, 0 },
                 new int[] { 2, 0 },
            };

            int[][] actual;
            actual = Tools.Extend(group, positives, negatives);
            Assert.IsTrue(Matrix.IsEqual(expected, actual));
        }
    }
}
