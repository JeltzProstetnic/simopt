// complete

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using MatthiasToolbox.Delta.Diff;

namespace MatthiasToolbox.Test.Delta
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.TextDelta.Trace and is intended
    ///to contain all BlueLogic.SDelta.Core.TextDelta.Trace Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class TraceTest
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
        ///A test for Add (int, int)
        ///</summary>
        [Test] // [TestMethod()]
        public void AddTest()
        {
            Trace target = new Trace(3);
            int x = 1;
            int y = 1;

            target.Add(x, y);

            Assert.AreEqual(1, target.Count);
        }

        /// <summary>
        ///A test for Count
        ///</summary>
        [Test] // [TestMethod()]
        public void CountTest()
        {
            int size = 3;
            Trace target = new Trace(size);
            target.Add(3, 3);
            int val = 1;

            Assert.AreEqual(val, target.Count, "BlueLogic.SDelta.Core.TextDelta.Trace.Count was not set correctly.");
        }

        /// <summary>
        ///A test for ToString ()
        ///</summary>
        [Test] // [TestMethod()]
        public void ToStringTest()
        {
            int size = 1;
            Trace target = new Trace(size);
            target.Add(1, 2);
            string expected = "{X=1,Y=2}";
            string actual;

            actual = target.ToString();

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.TextDelta.Trace.ToString did not return the expected value.");
        }


        /// <summary>
        ///A test for Equals (object)
        ///</summary>
        [Test] // [TestMethod()]
        public void EqualsTest()
        {
            int size = 0; // TODO: Initialize to an appropriate value

            Trace target = new Trace(size);

            object obj = null; // TODO: Initialize to an appropriate value

            bool expected = false;
            bool actual;

            actual = target.Equals(obj);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.TextDelta.Trace.Equals did not return the expected value.");
        }

        /// <summary>
        ///A test for MatchPoints ()
        ///</summary>
        [Test] // [TestMethod()]
        public void MatchPointsTest()
        {
            int size = 2;
            Trace target = new Trace(size);
            target.Add(1, 2);
            
            Point[] expected = new Point[2];
            expected[0] = new Point(1, 2);
            Point[] actual;

            actual = target.MatchPoints();

            CollectionAssert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.TextDelta.Trace.MatchPoints did not return the expected val" +
                    "ue.");
        }

        /// <summary>
        ///A test for operator != (Trace, Trace)
        ///</summary>
        [Test] // [TestMethod()]
        public void InequalityTest()
        {
            Trace a = new Trace(); // TODO: Initialize to an appropriate value

            Trace b = new Trace(); // TODO: Initialize to an appropriate value

            bool expected = false;
            bool actual;

            actual = a != b;

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.TextDelta.Trace.operator != did not return the expected val" +
                    "ue.");
        }

        /// <summary>
        ///A test for operator == (Trace, Trace)
        ///</summary>
        [Test] // [TestMethod()]
        public void EqualityTest()
        {
            Trace a = new Trace(12);
            Trace b = new Trace(13);

            bool expected = false;
            bool actual;

            actual = a == b;

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.TextDelta.Trace.operator == did not return the expected val" +
                    "ue.");
        }
    }
}