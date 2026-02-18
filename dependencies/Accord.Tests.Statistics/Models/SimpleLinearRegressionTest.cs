using Accord.Statistics.Models.Regression.Linear;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Accord.Statistics.Test
{


    /// <summary>
    ///This is a test class for SimpleLinearRegressionTest and is intended
    ///to contain all SimpleLinearRegressionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SimpleLinearRegressionTest
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
        ///A test for Regress
        ///</summary>
        [TestMethod()]
        public void RegressTest()
        {
            SimpleLinearRegression target = new SimpleLinearRegression();

            double[] inputs = { 80, 60, 10, 20, 30 };
            double[] outputs = { 20, 40, 30, 50, 60 };

            double eSlope = -0.264706;
            double eIntercept = 50.588235;


            target.Regress(inputs, outputs);

            double aSlope = target.Slope;
            double aIntercept = target.Intercept;

            Assert.AreEqual(eSlope, aSlope,0.0001);
            Assert.AreEqual(eIntercept, aIntercept,0.0001);
        }
    }
}
