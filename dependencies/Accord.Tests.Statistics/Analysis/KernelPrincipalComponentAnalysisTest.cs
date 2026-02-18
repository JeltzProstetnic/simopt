using Accord.Statistics.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Accord.Statistics.Kernels;
using Accord.Math;
using System.Diagnostics;

namespace Accord.Statistics.Test
{


    /// <summary>
    ///This is a test class for KernelPrincipalComponentAnalysisTest and is intended
    ///to contain all KernelPrincipalComponentAnalysisTest Unit Tests
    ///</summary>
    [TestClass()]
    public class KernelPrincipalComponentAnalysisTest
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
        ///A test for Transform
        ///</summary>
        [TestMethod()]
        public void TransformTest()
        {
            // Lindsay's tutorial data
            double[,] data = new double[,]
            {
                {2.5,   2.4},
                {0.5,	0.7},
                {2.2,	2.9},
                {1.9,	2.2},
                {3.1,	3.0},
                {2.3,	2.7},
                {2.0,	1.6},
                {1.0,	1.1},
                {1.5,	1.6},
                {1.1,	0.9}
            };

            // Using a linear kernel should be equivalent to standard PCA
            IKernel kernel = new Linear();

            // Create analysis
            KernelPrincipalComponentAnalysis target = new KernelPrincipalComponentAnalysis(data, kernel);

            // Compute
            target.Compute();

            double[,] actual = target.Transform(data, 2);

            // first inversed.. ?
            double[,] expected = new double[,]
            {
                { -0.827970186,  0.175115307 },
                {  1.77758033,  -0.142857227 },
                { -0.992197494, -0.384374989 },
                { -0.274210416, -0.130417207 },
                { -1.67580142,   0.209498461 },
                { -0.912949103, -0.175282444 },
                {  0.099109437,  0.349824698 },
                {  1.14457216,  -0.046417258 },
                {  0.438046137, -0.017764629 },
                {  1.22382056,   0.162675287 },
            };

            // Verify both are equal with 0.001 tolerance value
            Assert.IsTrue(Matrix.IsEqual(actual, expected, 0.0001));
        }

        /// <summary>
        ///A test for Revert
        ///</summary>
        [TestMethod()]
        public void RevertTest()
        {
            // Lindsay's tutorial data
            double[,] data = new double[,]
            {
                {2.5,   2.4},
                {0.5,	0.7},
                {2.2,	2.9},
                {1.9,	2.2},
                {3.1,	3.0},
                {2.3,	2.7},
                {2.0,	1.6},
                {1.0,	1.1},
                {1.5,	1.6},
                {1.1,	0.9}
            };

            // Using a linear kernel should be equivalent to standard PCA
            IKernel kernel = new Linear();

            // Create analysis
            KernelPrincipalComponentAnalysis target = new KernelPrincipalComponentAnalysis(data, kernel);

            // Compute
            target.Compute();

            // Compute image
            double[,] image = target.Transform(data, 2);

            // Compute preimage
            double[,] preimage = target.Revert(image);

            // Check if preimage equals the original data
            Assert.IsTrue(Matrix.IsEqual(data, preimage, 0.0001));
        }
            
    }
}
