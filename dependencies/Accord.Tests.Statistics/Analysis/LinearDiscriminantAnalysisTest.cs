using Accord.Statistics.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Accord.Math;
namespace Accord.Statistics.Test
{


    /// <summary>
    ///This is a test class for LinearDiscriminantAnalysisTest and is intended
    ///to contain all LinearDiscriminantAnalysisTest Unit Tests
    ///</summary>
    [TestClass()]
    public class LinearDiscriminantAnalysisTest
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
        ///A test for Compute
        ///</summary>
        [TestMethod()]
        public void ComputeTest()
        {
            // Gutierrez-Osuna example
            // http://research.cs.tamu.edu/prism/lectures/pr/pr_l10.pdf

            double[,] inputs = new double[,]
            {
                // Class 1
                { 4,1 },
                { 2,4 },
                { 2,3 },
                { 3,6 },
                { 4,4 },

                // Class 2
                {9,10},
                {6,8},
                {9,5},
                {8,7},
                {10,8}
            };

            int[] output = new int[] { 1, 1, 1, 1, 1,
                                       2, 2, 2, 2, 2 };

            LinearDiscriminantAnalysis target = new LinearDiscriminantAnalysis(inputs, output);
            target.Compute();

            Assert.IsTrue(Matrix.IsEqual(target.ClassScatter[0],
                new double[,] { { 0.80, -0.40 }, { -0.40, 2.64 } }, 0.01));

            Assert.IsTrue(Matrix.IsEqual(target.ClassScatter[1],
                new double[,] { { 1.84, -0.04 }, { -0.04, 2.64 } }, 0.01));

            Assert.IsTrue(Matrix.IsEqual(target.ScatterBetweenClass,
                new double[,] { { 29.16, 21.60 }, { 21.60, 16.00 } }, 0.01));

            Assert.IsTrue(Matrix.IsEqual(target.ScatterWithinClass,
                new double[,] { { 2.64, -0.44 }, { -0.44, 5.28 } },0.01));

        }
    }
}
