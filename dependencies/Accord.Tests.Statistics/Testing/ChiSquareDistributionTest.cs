using Accord.Statistics.Distributions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Accord.Statistics.Test
{
    
    
    /// <summary>
    ///This is a test class for ChiSquareDistributionTest and is intended
    ///to contain all ChiSquareDistributionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ChiSquareDistributionTest
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
        ///A test for ProbabilityDensityFunction
        ///</summary>
        [TestMethod()]
        public void ProbabilityDensityFunctionTest()
        {
            int degreesOfFreedom;
            double actual, expected, x;
            ChiSquareDistribution target;

            degreesOfFreedom = 1;
            target = new ChiSquareDistribution(degreesOfFreedom);
            x = 1;
            actual = target.ProbabilityDensityFunction(x);
            expected = 0.2420;
            Assert.AreEqual(expected, System.Math.Round(actual, 4));

            degreesOfFreedom = 2;
            target = new ChiSquareDistribution(degreesOfFreedom);
            x = 2;
            actual = target.ProbabilityDensityFunction(x);
            expected = 0.1839;
            Assert.AreEqual(expected, System.Math.Round(actual, 4));

            degreesOfFreedom = 10;
            target = new ChiSquareDistribution(degreesOfFreedom);
            x = 2;
            actual = target.ProbabilityDensityFunction(x);
            expected = 0.0077;
            Assert.AreEqual(expected, System.Math.Round(actual, 4));
        }

        /// <summary>
        ///A test for DistributionFunction
        ///</summary>
        [TestMethod()]
        public void DistributionFunctionTest()
        {
            int degreesOfFreedom;
            double actual, expected, x;
            ChiSquareDistribution target;

            degreesOfFreedom = 1;
            target = new ChiSquareDistribution(degreesOfFreedom);
            x = 5;
            actual = target.DistributionFunction(x);
            expected = 0.9747;
            Assert.AreEqual(expected, System.Math.Round(actual, 4));


            degreesOfFreedom = 5;
            target = new ChiSquareDistribution(degreesOfFreedom);
            x = 5;
            actual = target.DistributionFunction(x);
            expected = 0.5841;
            Assert.AreEqual(expected, System.Math.Round(actual, 4));
        }
    }
}
