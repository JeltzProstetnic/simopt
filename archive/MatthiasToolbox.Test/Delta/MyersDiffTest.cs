// complete

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MatthiasToolbox.Delta.Diff;

namespace MatthiasToolbox.Test.Delta
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.MyersDiff and is intended
    ///to contain all BlueLogic.SDelta.Core.MyersDiff Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class MyersDiffTest
    {
//        private TestContext testContextInstance;
//
//        /// <summary>
//        ///Gets or sets the test context which provides
//        ///information about and functionality for the current test run.
//        ///</summary>
//        public TestContext TestContext
//        {
//            get
//            {
//                return testContextInstance;
//            }
//            set
//            {
//                testContextInstance = value;
//            }
//        }

        /// <summary>
        /// A test for GetDiff (string, string, bool)
        /// testing single modifications
        ///</summary>
        [Test] // [TestMethod()]
        public void GetDiffTest1()
        {
            string text1 = "A,B,C".Replace(",", Environment.NewLine);
            string text2 = "A,B,C,D".Replace(",", Environment.NewLine);
            string result = "";
            String expected = "{X=0,Y=0}{X=1,Y=1}{X=2,Y=2}";

            MyersDiff target = new MyersDiff();
            result = target.GetDiff(text1, text2, true).ToString();

            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            text1 = "A,B,C,D".Replace(",", Environment.NewLine);
            text2 = "A,B,C".Replace(",", Environment.NewLine);
            result = "";
            expected = "{X=0,Y=0}{X=1,Y=1}{X=2,Y=2}";

            result = target.GetDiff(text1, text2, true).ToString();

            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            text1 = "X,A,B,C".Replace(",", Environment.NewLine);
            text2 = "A,B,C".Replace(",", Environment.NewLine);
            result = "";
            expected = "{X=1,Y=0}{X=2,Y=1}{X=3,Y=2}";

            result = target.GetDiff(text1, text2, true).ToString();

            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            text1 = "A,B,C".Replace(",", Environment.NewLine);
            text2 = "X,A,B,C".Replace(",", Environment.NewLine);
            result = "";
            expected = "{X=0,Y=1}{X=1,Y=2}{X=2,Y=3}";

            result = target.GetDiff(text1, text2, true).ToString();

            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            text1 = "A,B,C".Replace(",", Environment.NewLine);
            text2 = "A,B,D,C".Replace(",", Environment.NewLine);
            result = "";
            expected = "{X=0,Y=0}{X=1,Y=1}{X=2,Y=3}";

            result = target.GetDiff(text1, text2, true).ToString();

            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            text1 = "A,B,D,C".Replace(",", Environment.NewLine);
            text2 = "A,B,C".Replace(",", Environment.NewLine);
            result = "";
            expected = "{X=0,Y=0}{X=1,Y=1}{X=3,Y=2}";

            result = target.GetDiff(text1, text2, true).ToString();

            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");
        }

        /// <summary>
        /// A test for GetDiff (string, string, bool)
        /// testing D = 2 paths
        ///</summary>
        [Test] // [TestMethod()]
        public void GetDiffTest2()
        {
            // two modifications

            string text1 = "A,B,C".Replace(",", Environment.NewLine);
            string text2 = "A,B,C,D,D".Replace(",", Environment.NewLine);
            string result = "";
            string expected = "{X=0,Y=0}{X=1,Y=1}{X=2,Y=2}";

            MyersDiff target = new MyersDiff();

            result = target.GetDiff(text1, text2, true).ToString();

            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            text1 = "A,B,C,D,D".Replace(",", Environment.NewLine);
            text2 = "A,B,C".Replace(",", Environment.NewLine);
            result = "";
            expected = "{X=0,Y=0}{X=1,Y=1}{X=2,Y=2}";

            result = target.GetDiff(text1, text2, true).ToString();

            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            text1 = "X,X,A,B,C".Replace(",", Environment.NewLine);
            text2 = "A,B,C".Replace(",", Environment.NewLine);
            result = "";
            expected = "{X=2,Y=0}{X=3,Y=1}{X=4,Y=2}";

            result = target.GetDiff(text1, text2, true).ToString();

            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            text1 = "A,B,C".Replace(",", Environment.NewLine);
            text2 = "X,X,A,B,C".Replace(",", Environment.NewLine);
            result = "";
            expected = "{X=0,Y=2}{X=1,Y=3}{X=2,Y=4}";

            result = target.GetDiff(text1, text2, true).ToString();

            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            text1 = "X,A,X,B,C".Replace(",", Environment.NewLine);
            text2 = "A,B,C".Replace(",", Environment.NewLine);
            result = "";
            expected = "{X=1,Y=0}{X=3,Y=1}{X=4,Y=2}";

            result = target.GetDiff(text1, text2, true).ToString();

            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            text1 = "A,B,C".Replace(",", Environment.NewLine);
            text2 = "A,X,B,X,C".Replace(",", Environment.NewLine);
            result = "";
            expected = "{X=0,Y=0}{X=1,Y=2}{X=2,Y=4}";

            result = target.GetDiff(text1, text2, true).ToString();

            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            text1 = "A,B,X,X,C".Replace(",", Environment.NewLine);
            text2 = "A,B,C".Replace(",", Environment.NewLine);
            result = "";
            expected = "{X=0,Y=0}{X=1,Y=1}{X=4,Y=2}";

            result = target.GetDiff(text1, text2, true).ToString();

            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            text1 = "A,B,C".Replace(",", Environment.NewLine);
            text2 = "X,A,B,C,X".Replace(",", Environment.NewLine);
            result = "";
            expected = "{X=0,Y=1}{X=1,Y=2}{X=2,Y=3}";

            result = target.GetDiff(text1, text2, true).ToString();

            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

        }

        /// <summary>
        /// A test for GetDiff (string, string, bool)
        /// testing more complex paths
        ///</summary>
        [Test] // [TestMethod()]
        public void GetDiffTest3()
        {
            MyersDiff target = new MyersDiff();

            // ###### repro 1 
            string text1 = "A,B,C,A,B,B,A,A,B,C,B,B,A,A,B,A,B".Replace(",", Environment.NewLine);
            string text2 = "C,B,A,B,A,C".Replace(",", Environment.NewLine);
            string result = "";

            String expected = "{X=1,Y=1}{X=3,Y=2}{X=4,Y=3}{X=6,Y=4}{X=9,Y=5}";
            result = target.GetDiff(text1, text2, true).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // inversion
            expected = "{X=0,Y=2}{X=1,Y=4}{X=2,Y=6}{X=3,Y=8}{X=4,Y=15}";
            result = target.GetDiff(text2, text1, true).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // ###### reproduced a nasty error with an insert and a subsequent delete at the end of the script
            text1 = "A,B,C,A,B,X,B,A,A,B,C,B,A,A,B,A,B".Replace(",", Environment.NewLine);
            text2 = "C,B,Y,Y,A,B,A,C,Y".Replace(",", Environment.NewLine);

            expected = "{X=1,Y=1}{X=3,Y=4}{X=6,Y=5}{X=8,Y=6}{X=10,Y=7}";
            result = target.GetDiff(text1, text2, true).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // inversion
            expected = "{X=0,Y=10}{X=1,Y=11}{X=4,Y=13}{X=5,Y=14}{X=6,Y=15}";
            result = target.GetDiff(text2, text1, true).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // ###### repro kontrolle
            text1 = "A,B,C,A,B,X,B,A,A,B,C,B,A,A,B,A,B".Replace(",", Environment.NewLine);
            text2 = "A,B,C,A,B,B,A,A,B,C,B,B,A,A,B,A,B".Replace(",", Environment.NewLine);

            expected = "{X=0,Y=0}{X=1,Y=1}{X=2,Y=2}{X=3,Y=3}{X=4,Y=4}{X=6,Y=5}{X=7,Y=6}{X=8,Y=7}{X=9,Y=8}{X=10,Y=9}{X=11,Y=10}{X=12,Y=12}{X=13,Y=13}{X=14,Y=14}{X=15,Y=15}{X=16,Y=16}";
            result = target.GetDiff(text1, text2, true).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // inversion
            expected = "{X=0,Y=0}{X=1,Y=1}{X=2,Y=2}{X=3,Y=3}{X=4,Y=4}{X=5,Y=6}{X=6,Y=7}{X=7,Y=8}{X=8,Y=9}{X=9,Y=10}{X=10,Y=11}{X=12,Y=12}{X=13,Y=13}{X=14,Y=14}{X=15,Y=15}{X=16,Y=16}";
            result = target.GetDiff(text2, text1, true).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // ###### no matches
            text1 = "A,B,C,D,E,F,G,H,I,J,K".Replace(",", Environment.NewLine);
            text2 = "1,2,3,4,5,6,7,8,9,0".Replace(",", Environment.NewLine);

            expected = "";
            result = target.GetDiff(text1, text2, false).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // inversion
            expected = "";
            result = target.GetDiff(text2, text1, true).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");


            // ###### all matches
            text1 = "A,B,C,D,E,F,G,H,I,J,K".Replace(",", Environment.NewLine);
            text2 = text1;

            expected = "{X=0,Y=0}{X=1,Y=1}{X=2,Y=2}{X=3,Y=3}{X=4,Y=4}{X=5,Y=5}{X=6,Y=6}{X=7,Y=7}{X=8,Y=8}{X=9,Y=9}{X=10,Y=10}";
            result = target.GetDiff(text1, text2, false).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // ##### empty lines
            text1 = "A,B".Replace(",", Environment.NewLine);
            text2 = ",A,,,B,".Replace(",", Environment.NewLine);

            expected = "{X=0,Y=1}{X=1,Y=4}";
            result = target.GetDiff(text1, text2, false).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // inversion
            expected = "{X=1,Y=0}{X=4,Y=1}";
            result = target.GetDiff(text2, text1, false).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // ###### narrow corridor
            text1 = "F".Replace(",", Environment.NewLine);
            text2 = "0,F,1,2,3,4,5,6,7".Replace(",", Environment.NewLine);

            expected = "{X=0,Y=1}";
            result = target.GetDiff(text1, text2, false).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // inversion
            expected = "{X=1,Y=0}";
            result = target.GetDiff(text2, text1, false).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // ###### shifting 1
            text1 = "A,B,Y,C,D,E,F,F".Replace(",", Environment.NewLine);
            text2 = "A,B,X,C,E,F".Replace(",", Environment.NewLine);

            expected = "{X=0,Y=0}{X=1,Y=1}{X=3,Y=3}{X=5,Y=4}{X=6,Y=5}";
            result = target.GetDiff(text1, text2, false).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // inversion
            expected = "{X=0,Y=0}{X=1,Y=1}{X=3,Y=3}{X=4,Y=5}{X=5,Y=6}";
            result = target.GetDiff(text2, text1, false).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // ###### shifting 2
            text1 = "A,B,C,D,E,F,G,H,I,J,K".Replace(",", Environment.NewLine);
            text2 = "B,C,D,E,F,G,H,I,J,K,L".Replace(",", Environment.NewLine);

            expected = "{X=1,Y=0}{X=2,Y=1}{X=3,Y=2}{X=4,Y=3}{X=5,Y=4}{X=6,Y=5}{X=7,Y=6}{X=8,Y=7}{X=9,Y=8}{X=10,Y=9}";
            result = target.GetDiff(text1, text2, false).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // inversion
            expected = "{X=0,Y=1}{X=1,Y=2}{X=2,Y=3}{X=3,Y=4}{X=4,Y=5}{X=5,Y=6}{X=6,Y=7}{X=7,Y=8}{X=8,Y=9}{X=9,Y=10}";
            result = target.GetDiff(text2, text1, false).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // ###### broken path
            text1 = "1,A,2,B,C,D,E,G,H,I,J,3,K,L".Replace(",", Environment.NewLine);
            text2 = "4,A,5,B,C,D,E,6,E,G,H,I,J,7,K,8,L".Replace(",", Environment.NewLine);

            expected = "{X=1,Y=1}{X=3,Y=3}{X=4,Y=4}{X=5,Y=5}{X=6,Y=6}{X=7,Y=9}{X=8,Y=10}{X=9,Y=11}{X=10,Y=12}{X=12,Y=14}{X=13,Y=16}";
            result = target.GetDiff(text1, text2, false).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");

            // inversion
            expected = "{X=1,Y=1}{X=3,Y=3}{X=4,Y=4}{X=5,Y=5}{X=6,Y=6}{X=9,Y=7}{X=10,Y=8}{X=11,Y=9}{X=12,Y=10}{X=14,Y=12}{X=16,Y=13}";
            result = target.GetDiff(text2, text1, false).ToString();
            Assert.AreEqual(expected, result, "BlueLogic.SDelta.Core.MyersDiff.GetDiff did not return the expected value.");
        }


        /// <summary>
        ///A test for Indices1 ()
        ///</summary>
        [Test] // [TestMethod()]
        public void Indices1Test()
        {
            MyersDiff target = new MyersDiff();

            int[] expected = null;
            int[] actual;

            actual = target.Indices1();

            CollectionAssert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.TextDelta.MyersDiff.Indices1 did not return the expected va" +
                    "lue.");
        }

        /// <summary>
        ///A test for Indices2 ()
        ///</summary>
        [Test] // [TestMethod()]
        public void Indices2Test()
        {
            MyersDiff target = new MyersDiff();

            int[] expected = null;
            int[] actual;

            actual = target.Indices2();

            CollectionAssert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.TextDelta.MyersDiff.Indices2 did not return the expected va" +
                    "lue.");
        }

        /// <summary>
        ///A test for Length1
        ///</summary>
        [Test] // [TestMethod()]
        public void Length1Test()
        {
            MyersDiff target = new MyersDiff();

            int val = 0;

            Assert.AreEqual(val, target.Length1, "BlueLogic.SDelta.Core.TextDelta.MyersDiff.Length1 was not set correctly.");
        }

        /// <summary>
        ///A test for Length2
        ///</summary>
        [Test] // [TestMethod()]
        public void Length2Test()
        {
            MyersDiff target = new MyersDiff();

            int val = 0;

            Assert.AreEqual(val, target.Length2, "BlueLogic.SDelta.Core.TextDelta.MyersDiff.Length2 was not set correctly.");
        }
    } // class
} // namespace
