using Accord.Statistics.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Accord.Math;
namespace Accord.Statistics.Test
{


    /// <summary>
    ///This is a test class for PartialLeastSquaresAnalysisTest and is intended
    ///to contain all PartialLeastSquaresAnalysisTest Unit Tests
    ///</summary>
    [TestClass()]
    public class PartialLeastSquaresAnalysisTest
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
        public void NipalsComputeTest2()
        {
            // Data from Chiang, Y.Q., Zhuang, Y.M and Yang, J.Y, "Optimal Fisher discriminant analysis
            //      using the rank decomposition", Pattern Recognition, 25 (1992), 101--111.

            double[,] x1 = { // Class 1
                { 5.1, 3.5, 1.4, 0.2 }, { 4.9, 3.0, 1.4, 0.2 }, { 4.7, 3.2, 1.3, 0.2 }, { 4.6, 3.1, 1.5, 0.2 },
                { 5.0, 3.6, 1.4, 0.2 }, { 5.4, 3.9, 1.7, 0.4 }, { 4.6, 3.4, 1.4, 0.3 }, { 5.0, 3.4, 1.5, 0.2 },
                { 4.4, 2.9, 1.4, 0.2 }, { 4.9, 3.1, 1.5, 0.1 }, { 5.4, 3.7, 1.5, 0.2 }, { 4.8, 3.4, 1.6, 0.2 },
                { 4.8, 3.0, 1.4, 0.1 }, { 4.3, 3.0, 1.1, 0.1 }, { 5.8, 4.0, 1.2, 0.2 }, { 5.7, 4.4, 1.5, 0.4 },
                { 5.4, 3.9, 1.3, 0.4 }, { 5.1, 3.5, 1.4, 0.3 }, { 5.7, 3.8, 1.7, 0.3 }, { 5.1, 3.8, 1.5, 0.3 }, 
                { 5.4, 3.4, 1.7, 0.2 }, { 5.1, 3.7, 1.5, 0.4 }, { 4.6, 3.6, 1.0, 0.2 }, { 5.1, 3.3, 1.7, 0.5 }, 
                { 4.8, 3.4, 1.9, 0.2 }, { 5.0, 3.0, 1.6, 0.2 }, { 5.0, 3.4, 1.6, 0.4 }, { 5.2, 3.5, 1.5, 0.2 }, 
                { 5.2, 3.4, 1.4, 0.2 }, { 4.7, 3.2, 1.6, 0.2 }, { 4.8, 3.1, 1.6, 0.2 }, { 5.4, 3.4, 1.5, 0.4 }, 
                { 5.2, 4.1, 1.5, 0.1 }, { 5.5, 4.2, 1.4, 0.2 }, { 4.9, 3.1, 1.5, 0.2 }, { 5.0, 3.2, 1.2, 0.2 }, 
                { 5.5, 3.5, 1.3, 0.2 }, { 4.9, 3.6, 1.4, 0.1 }, { 4.4, 3.0, 1.3, 0.2 }, { 5.1, 3.4, 1.5, 0.2 }, 
                { 5.0, 3.5, 1.3, 0.3 }, { 4.5, 2.3, 1.3, 0.3 }, { 4.4, 3.2, 1.3, 0.2 }, { 5.0, 3.5, 1.6, 0.6 }, 
                { 5.1, 3.8, 1.9, 0.4 }, { 4.8, 3.0, 1.4, 0.3 }, { 5.1, 3.8, 1.6, 0.2 }, { 4.6, 3.2, 1.4, 0.2 }, 
                { 5.3, 3.7, 1.5, 0.2 }, { 5.0, 3.3, 1.4, 0.2 }
           };

            double[,] x2 = { // Class 2
                {7.0, 3.2, 4.7, 1.4 }, { 6.4, 3.2, 4.5, 1.5 }, { 6.9, 3.1, 4.9, 1.5 }, { 5.5, 2.3, 4.0, 1.3 },
                {6.5, 2.8, 4.6, 1.5 }, { 5.7, 2.8, 4.5, 1.3 }, { 6.3, 3.3, 4.7, 1.6 }, { 4.9, 2.4, 3.3, 1.0 },
                {6.6, 2.9, 4.6, 1.3 }, { 5.2, 2.7, 3.9, 1.4 }, { 5.0, 2.0, 3.5, 1.0 }, { 5.9, 3.0, 4.2, 1.5 },
                {6.0, 2.2, 4.0, 1.0 }, { 6.1, 2.9, 4.7 ,1.4 }, { 5.6, 2.9, 3.9, 1.3 }, { 6.7, 3.1, 4.4, 1.4 },
                {5.6, 3.0, 4.5, 1.5 }, { 5.8, 2.7, 4.1, 1.0 }, { 6.2, 2.2, 4.5, 1.5 }, { 5.6, 2.5, 3.9, 1.1 },
                {5.9, 3.2, 4.8, 1.8 }, { 6.1, 2.8, 4.0, 1.3 }, { 6.3, 2.5, 4.9, 1.5 }, { 6.1, 2.8, 4.7, 1.2 },
                {6.4, 2.9, 4.3, 1.3 }, { 6.6, 3.0, 4.4, 1.4 }, { 6.8, 2.8, 4.8, 1.4 }, { 6.7, 3.0, 5.0, 1.7 },
                {6.0, 2.9, 4.5, 1.5 }, { 5.7, 2.6, 3.5, 1.0 }, { 5.5, 2.4, 3.8, 1.1 }, { 5.5, 2.4, 3.7, 1.0 },
                {5.8, 2.7, 3.9, 1.2 }, { 6.0, 2.7, 5.1, 1.6 }, { 5.4, 3.0, 4.5, 1.5 }, { 6.0, 3.4, 4.5, 1.6 },
                {6.7, 3.1, 4.7, 1.5 }, { 6.3, 2.3, 4.4, 1.3 }, { 5.6, 3.0, 4.1, 1.3 }, { 5.5, 2.5, 5.0, 1.3 },
                {5.5, 2.6, 4.4, 1.2 }, { 6.1, 3.0, 4.6, 1.4 }, { 5.8, 2.6, 4.0, 1.2 }, { 5.0, 2.3, 3.3, 1.0 },
                {5.6, 2.7, 4.2, 1.3 }, { 5.7, 3.0, 4.2, 1.2 }, { 5.7, 2.9, 4.2, 1.3 }, { 6.2, 2.9, 4.3, 1.3 },
                {5.1, 2.5, 3.0, 1.1 }, { 5.7, 2.8, 4.1, 1.3 }
            };

            double[,] x3 = { // Class 3
                { 6.3, 3.3, 6.0, 2.5}, { 5.8, 2.7, 5.1, 1.9 }, { 7.1, 3.0, 5.9, 2.1 }, { 6.3, 2.9, 5.6, 1.8 },
                { 6.5, 3.0, 5.8, 2.2}, { 7.6, 3.0, 6.6, 2.1 }, { 4.9, 2.5, 4.5, 1.7 }, { 7.3, 2.9, 6.3, 1.8 }, 
                { 6.7, 2.5, 5.8, 1.8}, { 7.2, 3.6, 6.1, 2.5 }, { 6.5, 3.2, 5.1, 2.0 }, { 6.4, 2.7, 5.3, 1.9 },
                { 6.8, 3.0, 5.5, 2.1}, { 5.7, 2.5, 5.0, 2.0 }, { 5.8, 2.8, 5.1, 2.4 }, { 6.4, 3.2, 5.3, 2.3 },
                { 6.5, 3.0, 5.5, 1.8}, { 7.7, 3.8, 6.7, 2.2 }, { 7.7, 2.6, 6.9, 2.3 }, { 6.0, 2.2, 5.0, 1.5 },
                { 6.9, 3.2, 5.7, 2.3}, { 5.6, 2.8, 4.9, 2.0 }, { 7.7, 2.8, 6.7, 2.0 }, { 6.3, 2.7, 4.9, 1.8 },
                { 6.7, 3.3, 5.7, 2.1}, { 7.2, 3.2, 6.0, 1.8 }, { 6.2, 2.8, 4.8, 1.8 }, { 6.1, 3.0, 4.9, 1.8 },
                { 6.4, 2.8, 5.6, 2.1}, { 7.2, 3.0, 5.8, 1.6 }, { 7.4, 2.8, 6.1, 1.9 }, { 7.9, 3.8, 6.4, 2.0 },
                { 6.4, 2.8, 5.6, 2.2}, { 6.3, 2.8, 5.1, 1.5 }, { 6.1, 2.6, 5.6, 1.4 }, { 7.7, 3.0, 6.1, 2.3 },
                { 6.3 ,3.4, 5.6, 2.4}, { 6.4, 3.1, 5.5, 1.8 }, { 6.0, 3.0, 4.8, 1.8 }, { 6.9, 3.1, 5.4, 2.1 },
                { 6.7, 3.1, 5.6, 2.4}, { 6.9, 3.1, 5.1, 2.3 }, { 5.8, 2.7, 5.1, 1.9 }, { 6.8, 3.2, 5.9, 2.3 },
                { 6.7, 3.3, 5.7, 2.5}, { 6.7, 3.0, 5.2, 2.3 }, { 6.3, 2.5, 5.0, 1.9 }, { 6.5, 3.0, 5.2, 2.0 },
                { 6.2, 3.4, 5.4, 2.3}, { 5.9, 3.0, 5.1, 1.8 }
            };

            //Split data set into training (1:25) and testing (26:50)
            var idxTrain = Matrix.Indices(0, 25);
            var idxTest = Matrix.Indices(25, 50);

            double[,] inputs = Matrix.Combine(
                x1.Submatrix(idxTrain),
                x2.Submatrix(idxTrain),
                x3.Submatrix(idxTrain));


            double[,] outputs = Matrix.Expand(
                new double[,] { {1, 0, 0},
                                {0, 1, 0},
                                {0, 0, 1}},
                new int[] { 25, 25, 25 });


            PartialLeastSquaresAnalysis target = new PartialLeastSquaresAnalysis(inputs, outputs,
                AnalysisMethod.Standardize, PartialLeastSquaresAlgorithm.NIPALS);

            target.Compute();


            double[] xmean = target.Means[0];
            double[] xstdd = target.StandardDeviations[0];

            // Test X
            double[,] t = target.Projections[0];
            double[,] p = target.FactorMatrix[0];
            double[,] tp = t.Multiply(p.Transpose());
            for (int i = 0; i < tp.GetLength(0); i++)
                for (int j = 0; j < tp.GetLength(1); j++)
                    tp[i, j] = tp[i, j] * xstdd[j] + xmean[j];
            Assert.IsTrue(inputs.IsEqual(tp, 0.01));

            // Test Y
            double[] ymean = target.Means[1];
            double[] ystdd = target.StandardDeviations[1];
            double[,] u = target.Projections[1];
            double[,] q = target.FactorMatrix[1];
            double[,] uq = u.Multiply(q.Transpose());
            for (int i = 0; i < uq.GetLength(0); i++)
            {
                for (int j = 0; j < uq.GetLength(1); j++)
                {
                    uq[i, j] = uq[i, j] * ystdd[j] + ymean[j];
                }
            }

            Assert.IsTrue(Matrix.IsEqual(outputs, uq, 0.45));



            double[,] test = Matrix.Combine(
                x1.Submatrix(idxTest),
                x2.Submatrix(idxTest),
                x3.Submatrix(idxTest));

            // test regression for classification
            var regression = target.CreateRegression();

            double[][] Y = regression.Compute(test.ToArray());

            int c;
            Matrix.Max(Y[0], out c);
            Assert.AreEqual(0, c);

            Matrix.Max(Y[11], out c);
            Assert.AreEqual(0, c);

            Matrix.Max(Y[29], out c);
            Assert.AreEqual(1, c);

            Matrix.Max(Y[30], out c);
            Assert.AreEqual(1, c);

            Matrix.Max(Y[52], out c);
            Assert.AreEqual(2, c);

            Matrix.Max(Y[70], out c);
            Assert.AreEqual(2, c);


            PartialLeastSquaresAnalysis target2 = new PartialLeastSquaresAnalysis(inputs, outputs,
    AnalysisMethod.Standardize, PartialLeastSquaresAlgorithm.SIMPLS);

            target2.Compute();

            // First columns should be equal
            Assert.IsTrue(Matrix.IsEqual(
                target .Projections[0].GetColumn(0).Abs(),
                target2.Projections[0].GetColumn(0).Abs(), 0.00001));

            Assert.IsTrue(Matrix.IsEqual(
                target .FactorMatrix[0].GetColumn(0).Abs(),
                target2.FactorMatrix[0].GetColumn(0).Abs(), 0.00001));

            // Others are approximations
            Assert.IsTrue(Matrix.IsEqual(
                target .Projections[0].GetColumn(1).Abs(),
                target2.Projections[0].GetColumn(1).Abs(), 0.001));

            Assert.IsTrue(Matrix.IsEqual(
                target .FactorMatrix[0].GetColumn(1).Abs(),
                target2.FactorMatrix[0].GetColumn(1).Abs(), 0.01));

            // Explained variance proportion should be similar
            Assert.IsTrue(Matrix.IsEqual(
                target .FactorProportions[0].Submatrix(2),
                target2.FactorProportions[0].Submatrix(2), 0.05));
            Assert.IsTrue(Matrix.IsEqual(
                target .FactorProportions[1],
                target2.FactorProportions[1], 0.8));

        }

        /// <summary>
        ///A test for Compute
        ///</summary>
        [TestMethod()]
        public void SimplsComputeTest()
        {
            // Small example from Hervé Abdi
            // http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.76.399&rep=rep1&type=pdf

            double[,] dependent = {
                                      // Wine | Hedonic | Goes with meat | Goes with dessert
                                      {           14,          7,                 8 },
                                      {           10,          7,                 6 },
                                      {            8,          5,                 5 },
                                      {            2,          4,                 7 },
                                      {            6,          2,                 4 },
                                  };

            double[,] predictors = {
                                      // Wine | Price | Sugar | Alcohol | Acidity
                                               {   7,     7,      13,        7 },
                                               {   4,     3,      14,        7 },
                                               {  10,     5,      12,        5 },
                                               {  16,     7,      11,        3 },
                                               {  13,     3,      10,        3 },
                                   };


            PartialLeastSquaresAnalysis pls = new PartialLeastSquaresAnalysis(predictors, dependent,
                AnalysisMethod.Center, PartialLeastSquaresAlgorithm.SIMPLS);

            pls.Compute();

            double[,] eXL = {
                              { -9.45159018861170,	0.78968850262531,  -0.209368038058281,	-8.48528137423857 },
                              { -0.94757581483151,	3.88450085404821,	0.112930022780282,	-2.82842712474619 },
                              {  2.93870471647487,	0.74305572331951,  -0.901045382547656,	 2.12132034355964 },
                              {  3.88484814555025,	0.94386095018259,	0.130695802285997,	 2.82842712474619 }
                           };

            double[,] eXS = {
                              {  0.354624737683751,	 0.58596522018756,	 0.52998684526436,	0.00000000000000 },
                              {  0.616587298703812,	-0.34999998264191,	-0.49731289469286,	0.70710678118654 },
                              {  0.000000000000000,	 0.00000000000000,	 0.00000000000000,	0.00000000000000 },
                              { -0.591518691391630,	 0.38515999332448,	-0.50175433956929, -0.70710678118654 },
                              { -0.379693344995933, -0.62112523087013,	 0.46908038899779,	0.00000000000000 }
                           };

            double[,] eYL = {
                              {  7.66942186185178,	 1.74708185763490,   4.25766054162059,	-1.98028146359360 },
                              {  3.67302279915456,	 1.95014617437722,  -0.84013892628109,	-0.12877552870189 },
                              {  0.87711747396774,	 2.79934089543989,  -0.37994142703615,	-0.06581860355874 }
                           };

            double[,] eYS = {
                              {  55.1168117173553, 11.8189294056841,  9.96303195576597,	-0.0164546508896868 },
                              {  22.6848893220127, -6.7977742398925, -9.53292093809922,	-0.0164546508896868 },
                              {  -0.8771174739677, -2.7993408954398,  0.37994142703615,	 0.0658186035587468 },
                              { -48.8124364962975,  3.9819199686973, -9.61721006689343,	-0.0164546508896868 },
                              { -28.1121470691027, -6.2037342390488,  8.80715762219052,	-0.0164546508896868 }
                           };

            double[,] eW = {
                              {  -0.0827805451998377, 0.016405743311837, -0.256454560029589, -412411323076478 },
                              {   0.0028619204729086, 0.230476995894135,  0.199399046445959,  309308492307358 },
                              {   0.0250686073121819, 0.035160010682571, -0.999067234262154, -0.0027320814482 },
                              {   0.0377453269131196, 0.069534223826117,  0.180446153272915, -927925476922076 }
                           };


            double[,] eB = { // JAMA SVD
                              {  -1.69843117597223,  -0.0565410307596815, 0.0707830915722933 },
                              {   1.27383213308636,   0.2924040462656345, 0.5719119003970504 },
                              {  -4.00123870612780,   1.0002444265403554, 0.5001105385855153 },
                              {   1.17296538903427,   0.1238806837029775, 0.1597585129526230 },
                           };

            double[] eC = { 60.765188622652808, -8.5189464607305077, -4.3675094454974843 };

            double[] eProportionsX = { 0.863321875325239, 0.129969394829677, 0.006708729845084, 0.700757575757576 };
            double[] eProportionsY = { 0.676670955937952, 0.136034023774049, 0.175720946213926, 0.036503980559437 };


            double[,] aXL = pls.FactorMatrix[0];
            double[,] aYL = pls.FactorMatrix[1];
            double[,] aXS = pls.Projections[0];
            double[,] aYS = pls.Projections[1];
            double[,] aW = pls.Weights;
            var regression = pls.CreateRegression();
            double[,] aB = regression.Coefficients;
            double[] aC = regression.Intercepts;




            // The last singular value decomposition differs from the
            //  matlab version. However, the result is still valid.
            for (int i = 0; i < aXL.GetLength(0); i++)
                for (int j = 0; j < aXL.GetLength(1) - 1; j++)
                    Assert.AreEqual(aXL[i, j], eXL[i, j], 0.01);

            for (int i = 0; i < aYL.GetLength(0); i++)
                for (int j = 0; j < aYL.GetLength(1) - 1; j++)
                    Assert.AreEqual(aYL[i, j], eYL[i, j], 0.01);

            for (int i = 0; i < aYS.GetLength(0); i++)
                for (int j = 0; j < aYS.GetLength(1) - 1; j++)
                    Assert.AreEqual(aYS[i, j], eYS[i, j], 0.01);

            for (int i = 0; i < aXS.GetLength(0); i++)
                for (int j = 0; j < aXS.GetLength(1) - 1; j++)
                    Assert.AreEqual(aXS[i, j], eXS[i, j], 0.01);

            for (int i = 0; i < eW.GetLength(0); i++)
                for (int j = 0; j < eW.GetLength(1) - 1; j++)
                    Assert.AreEqual(aW[i, j], eW[i, j], 0.01);

            for (int i = 0; i < eB.GetLength(0); i++)
                for (int j = 0; j < eB.GetLength(1); j++)
                    Assert.AreEqual(aB[i, j], eB[i, j], 0.01);

            for (int i = 0; i < eC.Length; i++)
                Assert.AreEqual(aC[i], eC[i], 0.01);

            for (int i = 0; i < eProportionsX.Length - 1; i++)
            {
                Assert.AreEqual(eProportionsX[i], pls.FactorProportions[0][i], 0.1);
                Assert.AreEqual(eProportionsY[i], pls.FactorProportions[1][i], 0.1);
            }




            // Test Properties
            double[,] X0 = (double[,])pls.Sources[0].Clone(); Tools.Center(X0);
            double[,] Y0 = (double[,])pls.Sources[1].Clone(); Tools.Center(Y0);

            // XSCORES = X0*W
            double[,] X0W = X0.Multiply(aW);
            Assert.IsTrue(Matrix.IsEqual(aXS, X0W, 0.01));



            // Test Regression
            double[][] aY = regression.Compute(predictors.ToArray());

            double[] rSquared = regression.CoefficientOfDetermination(predictors.ToArray(), dependent.ToArray());



            for (int i = 0; i < dependent.GetLength(0); i++)
                for (int j = 0; j < dependent.GetLength(1); j++)
                {
                    double actual = aY[i][j];
                    double expected = dependent[i, j];

                    double d = System.Math.Abs(actual - expected);
                    double tol = 0.2 * expected;

                    Assert.IsTrue(d <= tol);
                }


        }

        /// <summary>
        ///A test for Compute
        ///</summary>
        [TestMethod()]
        public void NipalsComputeTest3()
        {
            // Example: taken from Geladi, P. and Kowalski, B.R., "An example of 2-block
            // predictive partial least-squares regression with simulated data",
            // Analytica Chemica Acta, 185(1996) 19--32.

            double[,] x = {
                         { 4, 9, 6, 7, 7, 8, 3, 2 },
                         { 6, 15, 10, 15, 17, 22, 9, 4 },
                         { 8, 21, 14, 23, 27, 36, 15, 6 },
                         { 10, 21, 14, 13, 11, 10, 3, 4 },
                         { 12, 27, 18, 21, 21, 24, 9, 6 },
                         { 14, 33, 22, 29, 31, 38, 15, 8 },
                         { 16, 33, 22, 19, 15, 12, 3, 6 },
                         { 18, 39, 26, 27, 25, 26, 9, 8 },
                         { 20, 45, 30, 35, 35, 40, 15, 10 }
                     };

            double[,] y = {
                        {1, 1},
                        {3, 1},
                        {5, 1},
                        {1, 3},
                        {3, 3},
                        {5, 3},
                        {1, 5},
                        {3, 5},
                        {5, 5}
                   };



            PartialLeastSquaresAnalysis pls = new PartialLeastSquaresAnalysis(x, y,
                AnalysisMethod.Center, PartialLeastSquaresAlgorithm.NIPALS);

            pls.Compute();


            double[,] eYL = { 
                           { 0.808248528018965,	-0.588841504103759},
                           { 0.588841504103759,	0.808248528018964}
                           };

            double[,] eYS = { 
                               { -2.79418006424545,	-0.438814047830411},
                               { -1.17768300820752,	-1.61649705603793},
                               { 0.438814047830411,	-2.79418006424545},
                               { -1.61649705603793,	1.17768300820752},
                               { 0,	0 },
                               { 1.61649705603793,	-1.17768300820752},
                               { -0.438814047830411,	2.79418006424545},
                               { 1.17768300820752,	1.61649705603793},
                               { 2.79418006424545,	0.438814047830411 }
                           };


            double[] eProportionsX = { 0.82623088878551032, 0.17376911121448976, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00 };
            double[] eProportionsY = { 0.50000000000000033, 0.50000000000000011, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00 };


            double[,] aXL = pls.FactorMatrix[0];
            double[,] aYL = pls.FactorMatrix[1];
            double[,] aXS = pls.Projections[0];
            double[,] aYS = pls.Projections[1];
            double[,] aW = pls.Weights;

            var regression = pls.CreateRegression();
            double[,] aB = regression.Coefficients;
            double[] aC = regression.Intercepts;



            for (int i = 0; i < eProportionsX.Length; i++)
            {
                Assert.AreEqual(eProportionsX[i], pls.FactorProportions[0][i], 0.01);
                Assert.AreEqual(eProportionsY[i], pls.FactorProportions[1][i], 0.01);
            }



            for (int i = 0; i < eYL.GetLength(0); i++)
                for (int j = 0; j < eYL.GetLength(1); j++)
                    Assert.AreEqual(aYL[i, j], eYL[i, j], 0.01);

            for (int i = 0; i < eYS.GetLength(0); i++)
                for (int j = 0; j < eYS.GetLength(1); j++)
                    Assert.AreEqual(aYS[i, j], eYS[i, j], 0.01);

        }

        /// <summary>
        ///A test for nipals
        ///</summary>
        [TestMethod()]
        [DeploymentItem("Accord.Statistics.dll")]
        public void NipalsComputeTest()
        {
            double[,] X = {
                              { 2.5, 2.4 },
                              { 0.5, 0.7 },
                              { 2.2, 2.9 },
                              { 1.9, 2.2 },
                              { 3.1, 3.0 },
                              { 2.3, 2.7 },
                              { 2.0, 1.6 },
                              { 1.0, 1.1 },
                              { 1.5, 1.6 },
                              { 1.1, 0.9 },
                          };

            double[,] Y = {
                              { 1 },
                              { 0 },
                              { 1 },
                              { 0 },
                              { 1 },
                              { 1 },
                              { 0 },
                              { 0 },
                              { 0 },
                              { 0 },
                          };


            PartialLeastSquaresAnalysis_Accessor target = new PartialLeastSquaresAnalysis_Accessor(X, Y,
                AnalysisMethod.Center, PartialLeastSquaresAlgorithm.NIPALS);

            double[,] X0 = X;
            double[,] Y0 = Y;

            target.Compute();

            double[,] x1 = target.scoresX.Multiply(target.loadingsX.Transpose()).Add(Tools.Mean(X), 0);
            double[,] y1 = target.scoresY.Multiply(target.loadingsY.Transpose()).Add(Tools.Mean(Y), 0);


            // XS*XL' ~ X0
            Assert.IsTrue(Matrix.IsEqual(x1, X, 0.01));

            // XS*YL' ~ Y0
            Assert.IsTrue(Matrix.IsEqual(y1, Y, 0.60));


            // ti' * tj = 0; 
            double[,] t = target.scoresX;
            for (int i = 0; i < t.GetLength(1); i++)
            {
                for (int j = 0; j < t.GetLength(1); j++)
                {
                    if (i != j)
                        Assert.AreEqual(0, t.GetColumn(i).InnerProduct(t.GetColumn(j)), 0.01);
                }
            }

            // wi' * wj = 0;
            double[,] w = target.weights;
            for (int i = 0; i < w.GetLength(1); i++)
            {
                for (int j = 0; j < w.GetLength(1); j++)
                {
                    if (i != j)
                        Assert.AreEqual(0, w.GetColumn(i).InnerProduct(w.GetColumn(j)), 0.01);
                }
            }

        }
    }
}
