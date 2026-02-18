using Accord.Statistics.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Accord.Math;
namespace Accord.Statistics.Test
{


    /// <summary>
    ///This is a test class for HiddenMarkovModelTest and is intended
    ///to contain all HiddenMarkovModelTest Unit Tests
    ///</summary>
    [TestClass()]
    public class HiddenMarkovModelTest
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
        public void LearnTest2()
        {
            int[][] sequences = new int[][] 
            {
                new int[] { 1, 2, 1 },
                new int[] { 1, 1, 1, 2, 1, 1, 1 },
                new int[] { 1, 1, 2, 2, 2, 2, 1, 2, 1, 1, 1 },
            };

            HiddenMarkovModel hmm = new HiddenMarkovModel(3, 3);


            hmm.Learn(sequences, 100);

            double l1 = hmm.Evaluate(new int[] { 1, 2, 1 });
            double l2 = hmm.Evaluate(new int[] { 1, 1, 1, 2, 2, 1 });
            double l3 = hmm.Evaluate(new int[] { 1, 1, 1, 1, 2, 2, 2, 2, 1, 1, 1 });
            double l4 = hmm.Evaluate(new int[] { 2, 1, 2 });
            double l5 = hmm.Evaluate(new int[] { 2, 1, 2, 2, 1, 2, 2, 2, 1, 2, 1 });

            Assert.AreEqual(0.05, l1, 0.005);
            Assert.AreEqual(0.0018, l2, 0.0005);
            Assert.IsTrue(l4 < 1e-15);


            HiddenMarkovModel hmm2 = new HiddenMarkovModel(3, 3);
            hmm2.Learn(sequences, 0.01);

            double ll1 = hmm.Evaluate(new int[] { 1, 2, 1 });
            double ll2 = hmm.Evaluate(new int[] { 1, 1, 1, 2, 2, 1 });
            double ll3 = hmm.Evaluate(new int[] { 1, 1, 1, 1, 2, 2, 2, 2, 1, 1, 1 });
            double ll4 = hmm.Evaluate(new int[] { 2, 1, 2 });
            double ll5 = hmm.Evaluate(new int[] { 2, 1, 2, 2, 1, 2, 2, 2, 1, 2, 1 });

            Assert.AreEqual(ll1, l1, 0.001);
            Assert.AreEqual(ll2, l2, 0.001);
            Assert.IsTrue(l4 < 1e-15);
        }

        /// <summary>
        ///A test for Learn
        ///</summary>
        [TestMethod()]
        public void LearnTest()
        {
            HiddenMarkovModel hmm = new HiddenMarkovModel(3, 2);

            int[] observation = new int[]
            { 
                0,1,1,2,2,1,1,1,0,0,0,0,0,0,0,0,2,2,0,0,1,1,1,2,0,0,
                0,0,0,0,1,2,1,1,1,0,2,0,1,0,2,2,2,0,0,2,0,1,2,2,0,1,
                1,2,2,2,0,0,1,1,2,2,0,0,2,2,0,0,1,0,1,2,0,0,0,0,2,0,
                2,0,1,1,0,1,0,0,0,1,2,1,1,2,0,2,0,2,2,0,0,1
            };

            int step2 = 650;
            int[] observation2 = new int[]
            {
                0,1,0,0,2,1,1,0,0,2,1,0,1,1,2,0,1,1,1,0,0,2,0,0,2,1,
                1,1,2,0,2,2,1,0,1,2,0,2,1,0,2,1,1,2,0,1,0,1,1,0,1,2,
                1,0,2,0,1,0,1,2,0,0,2,0,2,0,0,1,0,0,0,0,1,1,2,2,1,2,
                0,1,1,1,2,2,1,1,1,2,2,0,2,1,1,2,0,0,1,1,1,1,1,1,1,0,
                0,1,0,1,0,1,0,0,2,0,1,0,2,0,0,0,0,1,1,1,1,1,1,0,2,0,
                2,2,1,2,1,2,1,0,2,1,1,2,1,2,1,0,0,2,0,0,2,2,2,0,0,1,
                0,1,0,1,0,1,0,0,0,0,0,1,1,1,2,0,0,0,0,0,0,2,2,0,0,0,
                0,0,1,0,2,2,2,2,2,1,2,0,1,0,1,2,2,1,0,1,1,2,1,1,1,2,
                2,2,0,1,1,1,1,2,1,0,1,0,1,1,0,2,2,2,1,1,1,1,0,2,1,0,
                2,1,1,1,2,0,0,1,1,1,1,2,1,1,2,0,0,0,0,0,2,2,2,0,1,1,
                1,0,1,0,0,0,0,2,2,2,2,0,1,1,0,1,2,1,2,1,1,0,0,0,0,2,
                2,1,1,0,1,0,0,0,0,1,0,0,0,2,0,0,0,2,1,2,2,0,0,0,0,0,
                0,2,0,0,2,0,0,0,2,0,1,1,2,2,1,2,1,2,0,0,0,0,2,0,2,0,
                1,0,0,2,2,1,2,1,2,2,0,1,1,1,0,0,1,1,1,2,1,0,0,2,0,0,
                0,0,1,2,0,0,1,2,0,0,0,2,1,1,1,1,1,2,2,0,0,1,1,1,0,0,
                2,0,1,1,0,2,2,0,0,0,1,1,1,1,1,1,2,1,1,0,2,0,0,0,1,1,
                1,2,1,0,0,0,1,1,0,1,1,1,0,0,0,1,1,1,2,2,2,0,2,0,2,1,
                2,1,0,2,1,2,1,0,0,2,1,1,1,1,0,0,0,1,2,0,2,2,1,2,1,1,
                1,0,1,0,0,0,0,2,0,1,1,1,0,2,0,1,0,2,1,2,2,0,2,1,0,0,
                2,1,2,2,0,2,1,2,1,2,0,0,0,1,2,1,2,2,1,0,0,0,1,1,2,0,
                2,1,0,0,0,1,0,0,1,2,0,0,1,2,2,2,0,1,2,0,1,0,1,0,2,2,
                0,2,0,1,1,0,1,1,1,2,2,0,0,0,0,0,1,1,0,0,2,0,0,1,0,0,
                1,0,2,1,1,1,1,1,2,0,0,2,0,1,2,0,1,1,1,2,0,0,0,1,2,0,
                0,0,2,2,1,1,1,0,1,1,0,2,2,0,1,2,2,1,1,1,2,1,0,2,0,0,
                1,1,1,1,1,1,2,1,2,1,0,1,0,2,2,0,1,2,1,1,2,1,0,1,2,1
            };



            hmm.Learn(observation2, step2, 0.0);

            double[] pi = { 1.0, 0.0 };

            double[,] A =
            {
                { 0.7, 0.3 },
                { 0.5, 0.5 }
            };

            double[,] B =
            {
                { 0.6, 0.1, 0.3},
                { 0.1, 0.7, 0.2}
            };


            double tolerance = 0.20;
            Assert.IsTrue(Matrix.IsEqual(A, hmm.Transitions, tolerance));
            Assert.IsTrue(Matrix.IsEqual(B, hmm.Emissions, tolerance));
            Assert.IsTrue(Matrix.IsEqual(pi, hmm.Probabilities, tolerance));



        }




        /// <summary>
        ///A test for Learn
        ///</summary>
        [TestMethod()]
        public void LearnTest3()
        {
            int[][] sequences = new int[][] 
            {
                new int[] { 0,1,1,1,1,0,1,1,1,1 },
                new int[] { 0,1,1,1,0,1,1,1,1,1 },
                new int[] { 0,1,1,1,1,1,1,1,1,1 },
                new int[] { 0,1,1,1,1,1         },
                new int[] { 0,1,1,1,1,1,1       },
                new int[] { 0,1,1,1,1,1,1,1,1,1 },
                new int[] { 0,1,1,1,1,1,1,1,1,1 },
            };

            // Creates a new Hidden Markov Model with 3 states for
            //  an output alphabet of two characters (zero and one)
            HiddenMarkovModel hmm = new HiddenMarkovModel(2, 3);

            // Try to fit the model to the data until the difference in
            //  the average likelihood changes only by as little as 0.0001
            hmm.Learn(sequences, 0.0001);

            // Calculate the probability that the given
            //  sequences originated from the model
            double l1 = hmm.Evaluate(new int[] { 0, 1 });      // 0.49999423004045024  
            double l2 = hmm.Evaluate(new int[] { 0, 1, 1, 1 });  // 0.11458685045803882

            double l3 = hmm.Evaluate(new int[] { 1, 1 });      // 0.00000257134961090
            double l4 = hmm.Evaluate(new int[] { 1, 0, 0, 0 });  // 0.00000000000000000

            double l5 = hmm.Evaluate(new int[] { 0, 1, 0, 1, 1, 1, 1, 1, 1 }); // 0.00026743534097023312
            double l6 = hmm.Evaluate(new int[] { 0, 1, 1, 1, 1, 1, 1, 0, 1 }); // 0.00026743534097023312


            Assert.IsTrue(l1 > l3 && l1 > l4);
            Assert.IsTrue(l2 > l3 && l2 > l4);
        }

    }
}
