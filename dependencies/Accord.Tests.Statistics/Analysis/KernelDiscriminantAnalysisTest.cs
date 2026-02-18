using Accord.Statistics.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Accord.Statistics.Kernels;
using Accord.Math;

namespace Accord.Statistics.Test
{


    /// <summary>
    ///This is a test class for KernelDiscriminantAnalysisTest and is intended
    ///to contain all KernelDiscriminantAnalysisTest Unit Tests
    ///</summary>
    [TestClass()]
    public class KernelDiscriminantAnalysisTest
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
        public void KDAComputeTest()
        {
            double[,] inputs = {
                                   { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                                   { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 },
                               };

            int[] output = { 0, 1 };

            IKernel kernel = new Gaussian(0.1);

            KernelDiscriminantAnalysis target = new KernelDiscriminantAnalysis(inputs, output, kernel);
            target.Threshold = 0;

            target.Compute();


            double[,] actual = target.Transform(inputs);

            double[,] expected = 
            {
                { -0.70710678118654746, -0.70710678118654746},
                {  0.70710678118654746, -0.70710678118654746},
            };

            Assert.IsTrue(Matrix.IsEqual(actual, expected));

        }

        /// <summary>
        ///A test for Compute
        ///</summary>
        [TestMethod()]
        public void KDAComputeTest2()
        {
            // Scholkopf KPCA toy example
            double[,] inputs = {
                                    { -0.383504649, -0.162495898 },  { -0.437316092, -0.087483818 },
                                    { -0.492491985, -0.127135841 },  { -0.46483931,  -0.437745429 },
                                    { -0.569651254, -0.227378242 },  { -0.330385752, -0.232293992 },
                                    { -0.494094022, -0.168201208 },  { -0.320292822, -0.251117221 },
                                    { -0.473593147, -0.200204135 },  { -0.412832671, -0.039348904 },
                                    { -0.644617154, -0.115235137 },  { -0.570116535, -0.173189919 },
                                    { -0.375401788, -0.292348909 },  { -0.5638977,   -0.207049939 },
                                    { -0.442264978, -0.185210865 },  { -0.536002963, -0.255709364 },
                                    { -0.513557629, -0.23367057  },  { -0.634933848, -0.158477254 },
                                    { -0.62704499,  -0.044218646 },  { -0.401542973, -0.44442989  },
                                    { -0.504488061, -0.309819539 },  { -0.579894452, -0.087735214 },
                                    { -0.576517243, -0.141833274 },  { -0.41382651,  -0.22713543  },
                                    { -0.505622512, -0.158580869 },  { -0.448652183, -0.297781423 },
                                    { -0.460331913, -0.302146617 },  { -0.424378103, -0.168231202 },
                                    { -0.459951398, -0.04838922  },  { -0.634138072, -0.125056755 },
                                    { -0.050770039,  0.603988485 },  {  0.088529945,  0.351715749 },
                                    { -0.024809355,  0.715865471 },  { -0.0726249,	  0.497372053 },
                                    { -0.04450403,   0.715348699 },  { -0.061291112,  0.521354339 },
                                    { -0.020914408,	 0.663480859 },  {  0.056214783,  0.682040976 },
                                    { -0.106392289,  0.582397349 },  {  0.035158895,  0.656247387 },
                                    {  0.113299993,  0.587255712 },  {  0.014999425,  0.655417156 },
                                    {  0.070314405,  0.490265568 },  { -0.005241158,  0.52686986  },
                                    {  0.201849612,  0.740473192 },  {  0.09241594,	  0.537978579 },
                                    { -0.18141147,   0.623714877 },  {  0.003497332,  0.441315301 },
                                    { -0.180786206,  0.559851519 },  {  0.102819255,  0.522930773 },
                                    {  0.039460031,  0.573731949 },  {  0.063940564,  0.697648954 },
                                    {  0.087421289,  0.697781504 },  {  0.175240173,  0.717002111 },
                                    { -0.032005083,  0.615931086 },  { -0.013741381,  0.649952085 },
                                    {  0.061576963,  0.494462493 },  {  0.097789407,  0.55492568  },
                                    { -0.111534771,  0.727037824 },  { -0.055002145,  0.68986936  },
                                    {  0.54387051,   0.05261625  },  {  0.375265568, -0.018445412 },
                                    {  0.532466692,  0.019878283 },  {  0.539007041,  0.159042684 },
                                    {  0.459486168,  0.003219164 },  {  0.529231488,  0.088916367 },
                                    {  0.756591024, -0.129915249 },  {  0.454218436,  0.11825731  },
                                    {  0.338917299,  0.181747171 },  {  0.233047622, -0.058430213 },
                                    {  0.424030335, -0.101067382 },  {  0.432527914, -0.096049831 },
                                    {  0.382831281,  0.069115958 },  {  0.703293002, -0.075861821 },
                                    {  0.596848105, -0.009697173 },  {  0.5670292,	 -0.140694905 },
                                    {  0.542014604,  0.103081246 },  {  0.212724873, -0.07598744  },
                                    {  0.668587408,  0.087412723 },  {  0.502792455,  0.0761127   },
                                    {  0.409796942, -0.016592345 },  {  0.294674251,  0.030090744 },
                                    {  0.50890863,  -0.032246733 },  {  0.708709913, -0.036841128 },
                                    {  0.536511846,  0.114789528 },  {  0.584610553,  0.004143026 },
                                    {  0.481546234, -0.109804965 },  {  0.603071442,  0.156672375 },
                                    {  0.347237735, -0.104842345 },  {  0.596493896,  0.042272368 },
                               };

            int[] output = Matrix.Expand(new int[,] { { 1 }, { 2 }, { 3 } }, new int[] { 30, 30, 30 }).GetColumn(0);

            IKernel kernel = new Gaussian(0.2);

            KernelDiscriminantAnalysis target = new KernelDiscriminantAnalysis(inputs, output, kernel);

            target.Compute();


            double[,] actual = target.Transform(inputs, 2);

            double[,] expected1 = 
            {
                { -0.42278380586260333,  -0.10567191225106855},  // 0
                { -0.42677864812150623,  -0.10652141097959726},  // 1
                { -0.42914362032229258,  -0.1070275528834926},   // 2
            };


            Assert.IsTrue(Matrix.IsEqual(actual.Submatrix(0, 2, 0, 1), expected1));

        }

    }
}
