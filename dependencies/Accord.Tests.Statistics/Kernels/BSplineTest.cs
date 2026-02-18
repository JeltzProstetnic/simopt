using Accord.Statistics.Kernels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Accord.Statistics.Test
{
    
    
    /// <summary>
    ///This is a test class for BSplineTest and is intended
    ///to contain all BSplineTest Unit Tests
    ///</summary>
    [TestClass()]
    public class BSplineTest
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
        ///A test for Kernel
        ///</summary>
        [TestMethod()]
        public void KernelTest()
        {
            /*
            int order = 2;
            BSpline target = new BSpline(order);
            double[] x = { 1, 1, 1 };
            double[] y = { 1, 1, 1 };
            double expected = 287496;
            double actual;
            
            actual = target.Kernel(x, y);

            Assert.AreEqual(System.Math.Round(expected), System.Math.Round(actual));
             */ 
        }
    }
}
