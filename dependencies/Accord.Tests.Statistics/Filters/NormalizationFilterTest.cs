using Accord.Statistics.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace Accord.Statistics.Test
{


    /// <summary>
    ///This is a test class for NormalizationFilterTest and is intended
    ///to contain all NormalizationFilterTest Unit Tests
    ///</summary>
    [TestClass()]
    public class NormalizationFilterTest
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
        ///A test for Apply
        ///</summary>
        [TestMethod()]
        public void ApplyTest()
        {
            DataTable input = new DataTable("Sample data");
            input.Columns.Add("x", typeof(double));
            input.Columns.Add("y", typeof(double));
            input.Rows.Add(0.0, 0);
            input.Rows.Add(0.2, -20);
            input.Rows.Add(0.8, -80);
            input.Rows.Add(1.0, -100);

            DataTable expected = new DataTable("Sample data");
            expected.Columns.Add("x", typeof(double));
            expected.Columns.Add("y", typeof(double));
            expected.Rows.Add(-1.0502, 1.0502);
            expected.Rows.Add(-0.6301, 0.6301);
            expected.Rows.Add(0.6301, -0.6301);
            expected.Rows.Add(1.0502, -1.0502);



            NormalizationFilter target = new NormalizationFilter("x", "y");

            target.Detect(input);

            DataTable actual = target.Apply(input);

            for (int i = 0; i < actual.Rows.Count; i++)
            {
                double ex = (double)expected.Rows[i][0];
                double ey = (double)expected.Rows[i][1];

                double ax = (double)actual.Rows[i][0];
                double ay = (double)actual.Rows[i][1];

                Assert.AreEqual(ex, ax, 0.001);
                Assert.AreEqual(ey, ay, 0.001);

            }
        }
    }
}
