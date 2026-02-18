using Accord.Statistics.Models.Regression.Linear;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Accord.Statistics.Test
{
    
    
    /// <summary>
    ///This is a test class for MultipleLinearRegressionTest and is intended
    ///to contain all MultipleLinearRegressionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class MultipleLinearRegressionTest
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
            MultipleLinearRegression target = new MultipleLinearRegression(1,true);

            double[][] inputs = {
                                  new double[] {80},
                                  new double[] {60},
                                  new double[] {10},
                                  new double[] {20},
                                  new double[] {30},
                              };

            double[] outputs = { 20, 40, 30, 50, 60 };

            double eSlope = -0.264706;
            double eIntercept = 50.588235;


            target.Regress(inputs, outputs);

            double aSlope = target.Coefficients[0];
            double aIntercept = target.Coefficients[1];

            Assert.AreEqual(eSlope, aSlope, 0.0001);
            Assert.AreEqual(eIntercept, aIntercept, 0.0001);
        }
    }
}
