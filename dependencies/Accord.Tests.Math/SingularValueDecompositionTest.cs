using Accord.Math.Decompositions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Accord.Math.Test
{
    
    
    /// <summary>
    ///This is a test class for SingularValueDecompositionTest and is intended
    ///to contain all SingularValueDecompositionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SingularValueDecompositionTest
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
        ///A test for Inverse
        ///</summary>
        [TestMethod()]
        public void InverseTest()
        {
            double[,] value = new double[,]
                { { 1.0, 1.0 },
                  { 2.0, 2.0 }  };

            SingularValueDecomposition target = new SingularValueDecomposition(value);
            
            double[,] expected = new double[,]
                { { 0.1, 0.2 },
                  { 0.1, 0.2 }  };
    
            double[,] actual = target.Solve(Matrix.Identity(2));

            Assert.IsTrue(Matrix.IsEqual(expected,actual,0.001));

            actual = target.Inverse();

            Assert.IsTrue(Matrix.IsEqual(expected, actual, 0.001));
        }

        /// <summary>
        ///A test for SingularValueDecomposition Constructor
        ///</summary>
        [TestMethod()]
        public void SingularValueDecompositionConstructorTest1()
        {
            double[,] value = new double[,]
             { 
                 { 1, 2 },
                 { 3, 4 },
                 { 5, 6 },
                 { 7, 8 }
             }.Transpose();

            SingularValueDecomposition target = new SingularValueDecomposition(value);

            double[,] actual = target.LeftSingularVectors.Multiply(
                Matrix.Diagonal(target.Diagonal)).Multiply(target.RightSingularVectors.Transpose());

            Assert.IsTrue(Matrix.IsEqual(actual, value, 0.01));
        }

    }
}
