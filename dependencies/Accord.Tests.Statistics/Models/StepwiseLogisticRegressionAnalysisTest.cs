using Accord.Statistics.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Accord.Math;
namespace Accord.Statistics.Test
{


    /// <summary>
    ///This is a test class for StepwiseLogisticRegressionAnalysisTest and is intended
    ///to contain all StepwiseLogisticRegressionAnalysisTest Unit Tests
    ///</summary>
    [TestClass()]
    public class StepwiseLogisticRegressionAnalysisTest
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
        ///A test for ComputeRound
        ///</summary>
        [TestMethod()]
        public void DoStepTest()
        {

            double[][] inputs = Matrix.Expand(
                new double[][] {
                    new double[] {0, 0, 0},
                    new double[] {1, 0, 0},
                    new double[] {0, 1, 0},
                    new double[] {1, 1, 0},
                    new double[] {0, 0, 1},
                    new double[] {1, 0, 1},
                    new double[] {0, 1, 1},
                    new double[] {1, 1, 1},
                }, new int[] { 60, 17, 8, 2, 187, 85, 51, 23 });

            double[] outputs = Matrix.Expand(
                new double[] { 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0 },
                new int[] { 5, 60 - 5, 2, 17 - 2, 1, 8 - 1, 0, 2 - 0, 35, 187 - 35, 13, 85 - 13, 15, 51 - 15, 8, 23 - 8 });



            var target2 = new LogisticRegressionAnalysis(inputs, outputs);
            target2.Compute();

            Assert.AreEqual(target2.CoefficientValues[0], -2.377661, 0.0001);
            Assert.AreEqual(target2.CoefficientValues[1], -0.067775, 0.0001);
            Assert.AreEqual(target2.CoefficientValues[2], 0.69531, 0.0001);
            Assert.AreEqual(target2.CoefficientValues[3], 0.871939, 0.0001);


            var target = new StepwiseLogisticRegressionAnalysis(
                inputs, outputs,
                new string[] { "x1", "x2", "x3" }, "Y"
            );

            target.Threshold = 0.15;

            int actual;
            actual = target.DoStep();
            Assert.AreEqual(0, actual);

            actual = target.DoStep();
            Assert.AreEqual(-1, actual);


        }
    }
}
