using Accord.Statistics.Kernels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Accord.Statistics.Test
{
    
    
    /// <summary>
    ///This is a test class for SplineTest and is intended
    ///to contain all SplineTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SplineTest
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

        /*
        /// <summary>
        ///A test for Kernel
        ///</summary>
        [TestMethod()]
        public void KernelTest()
        {
            Spline target = new Spline();

            double[] x = { 1, 2, 3 };
            double[] y = { 4, 5, 6 };
            double expected = 5.577138888888889e+03;
            double actual;

            actual = target.Kernel(x, y);
          //  Assert.AreEqual(expected, actual);


            double[] x2 = { 5, 11, 2 };
            double[] y2 = { 4, 5,  1 };
            expected = 3.331507407407407e+04;

            actual = target.Kernel(x2, y2);
           // Assert.AreEqual(System.Math.Round(expected, 5),
           //                 System.Math.Round(actual, 5));
        }
  */
    }
}
