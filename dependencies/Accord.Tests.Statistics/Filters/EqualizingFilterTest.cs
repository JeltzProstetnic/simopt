using Accord.Statistics.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace Accord.Statistics.Test
{
    
    
    /// <summary>
    ///This is a test class for EqualizingFilterTest and is intended
    ///to contain all EqualizingFilterTest Unit Tests
    ///</summary>
    [TestClass()]
    public class EqualizingFilterTest
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
            DataTable data = new DataTable("Sample data");
            data.Columns.Add("x", typeof(double));
            data.Columns.Add("Class", typeof(int));
            data.Rows.Add(0.21, 0);
            data.Rows.Add(0.25, 0);
            data.Rows.Add(0.54, 0);
            data.Rows.Add(0.19, 1);

            DataTable expected = new DataTable("Sample data");
            expected.Columns.Add("x", typeof(double));
            expected.Columns.Add("Class", typeof(int));
            expected.Rows.Add(0.21, 0);
            expected.Rows.Add(0.25, 0);
            expected.Rows.Add(0.54, 0);
            expected.Rows.Add(0.19, 1);
            expected.Rows.Add(0.19, 1);
            expected.Rows.Add(0.19, 1);


            DataTable actual;

            EqualizingFilter target = new EqualizingFilter("Class");
            target.ColumnOptions["Class"].Classes = new int[] { 0, 1 };
            
            actual = target.Apply(data);

            for (int i = 0; i < actual.Rows.Count; i++)
            {
                double ex = (double)expected.Rows[i][0];
                int ec = (int)expected.Rows[i][1];

                double ax = (double)actual.Rows[i][0];
                int ac = (int)actual.Rows[i][1];

                Assert.AreEqual(ex, ax);
                Assert.AreEqual(ec, ac);                    
                
            }
            
        }
    }
}
