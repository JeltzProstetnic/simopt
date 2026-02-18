using Accord.Statistics.Models.Regression.Linear;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Accord.Statistics.Test
{


    /// <summary>
    ///This is a test class for MultivariateLinearRegressionTest and is intended
    ///to contain all MultivariateLinearRegressionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class MultivariateLinearRegressionTest
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
            double[][] X = {
                               new double[] {    4.47 },
                               new double[] {  208.30 },
                               new double[] { 3400.00 },
                           };


            double[][] Y = {
                               new double[] {    0.51 },
                               new double[] {  105.66 },
                               new double[] { 1800.00 },
                         };



            double eB = 0.5303528166;
            double eA = -3.290915095;

            MultivariateLinearRegression target = new MultivariateLinearRegression(1, 1, true);

            target.Regress(X, Y);

            Assert.AreEqual(target.Coefficients[0, 0], eB, 0.001);
            Assert.AreEqual(target.Intercepts[0], eA, 0.001);

            Assert.AreEqual(target.Inputs, 1);
            Assert.AreEqual(target.Outputs, 1);



            // Test manually including an constant term to generate an intercept
            double[][] X1 = {
                               new double[] {    4.47, 1 },
                               new double[] {  208.30, 1 },
                               new double[] { 3400.00, 1 },
                           };

            MultivariateLinearRegression target2 = new MultivariateLinearRegression(2, 1, false);

            target2.Regress(X1, Y);

            Assert.AreEqual(target2.Coefficients[0, 0], eB, 0.001);
            Assert.AreEqual(target2.Coefficients[1, 0], eA, 0.001);

            Assert.AreEqual(target2.Inputs,  2);
            Assert.AreEqual(target2.Outputs, 1);
        }
    }
}
