using Accord.Math;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Accord.Math.Test
{


    /// <summary>
    ///This is a test class for SpecialTest and is intended
    ///to contain all SpecialTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SpecialTest
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
        ///A test for Binomial
        ///</summary>
        [TestMethod()]
        public void BinomialTest()
        {
            int n = 63;
            int k = 6;

            double expected = 67945521;
            double actual;

            actual = Special.Binomial(n, k);
            Assert.AreEqual(expected, actual);

            n = 42;
            k = 12;
            expected = 11058116888;

            actual = Special.Binomial(n, k);
            Assert.AreEqual(expected, actual);
        }


        /// <summary>
        ///A test for BSpline
        ///</summary>
        [TestMethod()]
        public void BSplineTest()
        {
           
        }

    }
}
