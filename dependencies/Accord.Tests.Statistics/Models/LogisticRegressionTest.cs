using Accord.Statistics.Models.Regression;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Accord.Math;
using Accord.Statistics.Analysis;
namespace Accord.Statistics.Test
{


    /// <summary>
    ///This is a test class for LogisticRegressionTest and is intended
    ///to contain all LogisticRegressionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class LogisticRegressionTest
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
        ///A test for Regress
        ///</summary>
        [TestMethod()]
        public void RegressTest()
        {

            double[,] inputGrouped = {
                           { 1, 4, 5 }, // product 1 has four occurances of class 1 and five  of class 0
                           { 2, 1, 3 }, // product 2 has one  occurance  of class 1 and three of class 0
                          };

            double[,] inputGroupProb = {
                               { 1, 4.0/(4+5) },
                               { 2, 1.0/(1+3) },
                           };


            double[,] inputExtended = {
                 { 1, 1 }, // observation of product 1 in class 1
                 { 1, 1 }, // observation of product 1 in class 1
                 { 1, 1 }, // observation of product 1 in class 1
                 { 1, 1 }, // observation of product 1 in class 1
                 { 1, 0 }, // observation of product 1 in class 0
                 { 1, 0 }, // observation of product 1 in class 0
                 { 1, 0 }, // observation of product 1 in class 0
                 { 1, 0 }, // observation of product 1 in class 0
                 { 1, 0 }, // observation of product 1 in class 0
                 { 2, 1 }, // observation of product 2 in class 1
                 { 2, 0 }, // observation of product 2 in class 0
                 { 2, 0 }, // observation of product 2 in class 0
                 { 2, 0 }, // observation of product 2 in class 0
            };


            // Fit using extended data
            double[][] inputs = Matrix.ColumnVector(inputExtended.GetColumn(0)).ToArray();
            double[] outputs = inputExtended.GetColumn(1);
            LogisticRegression target = new LogisticRegression(1);
            target.Regress(inputs, outputs);

            // Fit using grouped data
            double[][] inputs2 = Matrix.ColumnVector(inputGroupProb.GetColumn(0)).ToArray();
            double[] outputs2 = inputGroupProb.GetColumn(1);
            LogisticRegression target2 = new LogisticRegression(1);
            target2.Regress(inputs2, outputs2);


            Assert.IsTrue(Matrix.IsEqual(target.Coefficients, target2.Coefficients, 0.000001)); // OK!



            double[,] data = new double[,] {
                { 1,0 },
                { 2,0 },
                { 3,0 },
                { 4,0 },
                { 5,1 },
                { 6,0 },
                { 7,1 },
                { 8,0 },
                { 9,1 },
                { 10,1 }
        };


            double[][] inputs3 = Matrix.ColumnVector(data.GetColumn(0)).ToArray();
            double[] outputs3 = data.GetColumn(1);
            LogisticRegressionAnalysis analysis = new LogisticRegressionAnalysis(inputs3, outputs3);

            analysis.Compute();

            Assert.AreEqual(analysis.Deviance, 8.6202, 0.0005);
            Assert.AreEqual(analysis.ChiSquare.PValue, 0.0278, 0.0005);

            // Check intercept
            Assert.AreEqual(analysis.Coefficients[0].Coefficient, -4.3578, 0.0005);

            // Check coefficients
            Assert.AreEqual(analysis.Coefficients[1].Coefficient, 0.6622, 0.0005);

            // Check statistics
            Assert.AreEqual(analysis.Coefficients[1].StandardError, 0.4001, 0.0005);
            Assert.AreEqual(analysis.Coefficients[1].Wald.PValue, 0.0979, 0.0005);

            Assert.AreEqual(analysis.Coefficients[1].OddsRatio, 1.9391, 0.0005);
            Assert.AreEqual(analysis.Coefficients[1].ConfidenceLower, 0.8852, 0.0005);
            Assert.AreEqual(analysis.Coefficients[1].ConfidenceUpper, 4.2478, 0.0005);


        }
    }
}
