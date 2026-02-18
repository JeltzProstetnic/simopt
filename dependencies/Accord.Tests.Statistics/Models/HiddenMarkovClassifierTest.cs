using Accord.Statistics.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Accord.Statistics.Test
{


    /// <summary>
    ///This is a test class for HiddenMarkovClassifierTest and is intended
    ///to contain all HiddenMarkovClassifierTest Unit Tests
    ///</summary>
    [TestClass()]
    public class HiddenMarkovClassifierTest
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
        ///A test for Learn
        ///</summary>
        [TestMethod()]
        public void LearnTest()
        {
            // Declare some testing data
            int[][] inputs = new int[][]
            {
                new int[] { 0,1,1,0 },   // Class 0
                new int[] { 0,0,1,0 },   // Class 0
                new int[] { 0,1,1,1,0 }, // Class 0
                new int[] { 0,1,0 },     // Class 0

                new int[] { 1,0,0,1 },   // Class 1
                new int[] { 1,1,0,1 },   // Class 1
                new int[] { 1,0,0,0,1 }, // Class 1
                new int[] { 1,0,1 },     // Class 1
            };

            int[] outputs = new int[]
            {
                0,0,0,0, // First four sequences are of class 0
                1,1,1,1, // Last four sequences are of class 1
            };


            // We are trying to predict two different classes
            int classes = 2;

            // Each sequence may have up to two symbols (0 or 1)
            int symbols = 2;

            // Nested models will have two states each
            int[] states = new int[] { 2, 2 };

            // Creates a new Hidden Markov Model Classifier with the given parameters
            MarkovSequenceClassifier hmc = new MarkovSequenceClassifier(classes, symbols, states);


            // Will train until convergence of the average likelihood
            double likelihood = hmc.Learn(inputs, outputs, 0.001);


            // Will assert the models have learned the sequences correctly.
            for (int i = 0; i < inputs.Length; i++)
            {
                int expected = outputs[i];
                int actual = hmc.Compute(inputs[i], out likelihood);
                Assert.AreEqual(expected, actual);
            }
        }
    }
}
