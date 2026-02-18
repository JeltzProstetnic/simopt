using Accord.Statistics.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Accord.Math;
namespace Accord.Statistics.Test
{


    /// <summary>
    ///This is a test class for ConfusionMatrixTest and is intended
    ///to contain all ConfusionMatrixTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ConfusionMatrixTest
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
        ///A test for ConfusionMatrix Constructor
        ///</summary>
        [TestMethod()]
        public void ConfusionMatrixConstructorTest()
        {
            // System output
            int[] predicted = new int[] { 0, 0, 0, 1, 1, 0, 0, 0, 0, 1 };

            // Corret output
            int[] expected  = new int[] { 0, 0, 1, 0, 1, 0, 0, 0, 0, 0 };

            // 1 means positive, 0 means negative
            int positiveValue = 1;
            int negativeValue = 0;

            ConfusionMatrix target = new ConfusionMatrix(predicted, expected, positiveValue, negativeValue);

            int falseNegatives = 1;
            int falsePositives = 2;
            int truePositives = 1;
            int trueNegatives = 6;

            Assert.AreEqual(predicted.Length, target.Observations);
            Assert.AreEqual(8, target.ActualNegatives);
            Assert.AreEqual(2, target.ActualPositives);
            Assert.AreEqual(7, target.PredictedNegatives);
            Assert.AreEqual(3, target.PredictedPositives);

            Assert.AreEqual(falseNegatives, target.FalseNegatives);
            Assert.AreEqual(falsePositives, target.FalsePositives);
            Assert.AreEqual(truePositives, target.TruePositives);
            Assert.AreEqual(trueNegatives, target.TrueNegatives);

        }
    }
}
