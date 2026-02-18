using Accord.Statistics.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace Accord.Statistics.Test
{
    
    
    /// <summary>
    ///This is a test class for CategoryFilterTest and is intended
    ///to contain all CategoryFilterTest Unit Tests
    ///</summary>
    [TestClass()]
    public class CategoryFilterTest
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
            CategoryFilter target = new CategoryFilter();
            

            DataTable input = new DataTable("Sample data");
            
            input.Columns.Add("Age", typeof(int));
            input.Columns.Add("Classification", typeof(string));

            input.Rows.Add(10, "child");
            input.Rows.Add(7,  "child");
            input.Rows.Add(4,  "child");
            input.Rows.Add(21, "adult");
            input.Rows.Add(27, "adult");
            input.Rows.Add(12, "child");
            input.Rows.Add(79, "elder");
            input.Rows.Add(40, "adult");
            input.Rows.Add(30, "adult");



            DataTable expected = new DataTable("Sample data");

            expected.Columns.Add("Age", typeof(int));
            expected.Columns.Add("Classification", typeof(int));

            expected.Rows.Add(10, 0);
            expected.Rows.Add(7, 0);
            expected.Rows.Add(4, 0);
            expected.Rows.Add(21, 1);
            expected.Rows.Add(27, 1);
            expected.Rows.Add(12, 0);
            expected.Rows.Add(79, 2);
            expected.Rows.Add(40, 1);
            expected.Rows.Add(30, 1);



            // Detect the mappings
            target.Detect(input);

            // Apply the categorization
            DataTable actual = target.Apply(input);


            for (int i = 0; i < actual.Rows.Count; i++)
            {
                for (int j = 0; j < actual.Columns.Count; j++)
                {
                    Assert.AreEqual(expected.Rows[i][j], actual.Rows[i][j]);
                }
            }

        }
    }
}
