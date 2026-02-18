using Accord.Statistics.Models.Regression.Linear;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Accord.Statistics.Tests
{
    
    
    /// <summary>
    ///This is a test class for PolynomialRegressionTest and is intended
    ///to contain all PolynomialRegressionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class PolynomialRegressionTest
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
        public void PolynomialRegressionRegressTest()
        {
            double[] inputs = {15.2, 229.7, 3500};
            double[] outputs = {0.51, 105.66, 1800};

            int degree = 2;
            PolynomialRegression target = new PolynomialRegression(degree);

            double[] expected = { 8.003175717e-6, 4.882498125e-1, -6.913246203 };
            double[] actual;

            target.Regress(inputs, outputs);
            actual = target.Coefficients;

            Assert.AreEqual(expected[0], actual[0],000.1);
            Assert.AreEqual(expected[1], actual[1],000.1);
            Assert.AreEqual(expected[2], actual[2],000.1);
        }
    }
}
