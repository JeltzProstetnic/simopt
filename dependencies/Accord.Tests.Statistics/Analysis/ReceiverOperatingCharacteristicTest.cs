using Accord.Statistics.Analysis;
using Accord.Math;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Accord.Statistics.Test
{


    /// <summary>
    ///This is a test class for ReceiverOperatingCharacteristicTest and is intended
    ///to contain all ReceiverOperatingCharacteristicTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ReceiverOperatingCharacteristicTest
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
        ///A test for Compute
        ///</summary>
        [TestMethod()]
        public void ComputeTest()
        {

            // Example from
            // http://faculty.vassar.edu/lowry/roc1.html

            double[,] data = { 
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 },
                               { 4,  1 }, // 18
                               { 4,  0 },

                               { 6,  1 }, 
                               { 6,  1 }, 
                               { 6,  1 }, 
                               { 6,  1 }, 
                               { 6,  1 }, 
                               { 6,  1 }, 
                               { 6,  1 }, // 7
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 },
                               { 6,  0 }, // 17

                               { 8,  1 },
                               { 8,  1 },
                               { 8,  1 },
                               { 8,  1 }, // 4
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 },
                               { 8,  0 }, // 36

                               { 9, 1 },
                               { 9, 1 },
                               { 9, 1 }, // 3
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 },
                               { 9, 0 }, 
                               { 9, 0 }, 
                               { 9, 0 }, 
                               { 9, 0 }, // 39
                             };


            double[] measurement = data.GetColumn(1);
            double[] prediction = data.GetColumn(0);

            ReceiverOperatingCharacteristic roc = new ReceiverOperatingCharacteristic(measurement, prediction); // TODO: Initialize to an appropriate value
            double[] cutpoints = { 5, 7, 9 };

            roc.Compute(cutpoints);

            var p1 = roc.Points[0];
            var p2 = roc.Points[1];
            var p3 = roc.Points[2];

            double area = roc.Area;
            double error = roc.Error;

            // Area should be near 0.87
            Assert.IsTrue(area > 0.86 && area < 0.88);
        }
    }
}
