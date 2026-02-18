// complete

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using MatthiasToolbox.Utilities.IO;

namespace MatthiasToolbox.Test.Utilities
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.SlidingStreamWindow and is intended
    ///to contain all BlueLogic.SDelta.Core.SlidingStreamWindow Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class SlidingStreamWindowTest
    {
//        private TestContext testContextInstance;
        private static byte[] testData = Encoding.UTF8.GetBytes("BlueLogic Software Solutions");

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
        ///A test for Finished
        ///</summary>
        [Test] // [TestMethod()]
        public void FinishedTest()
        {
            Stream inputStream = new MemoryStream(testData);
            int windowSize = 7;
            int blocksToBuffer = 2;

            SlidingStreamWindow target = new SlidingStreamWindow(inputStream, windowSize, blocksToBuffer);

            Assert.AreEqual(false, target.Finished, "BlueLogic.SDelta.Core.SlidingStreamWindow.Finished was not set correctly.");
            foreach(byte[] block in target)
            {
                byte[] dummy = block; 
            }
            Assert.AreEqual(true, target.Finished, "BlueLogic.SDelta.Core.SlidingStreamWindow.Finished was not set correctly.");
        }

        /// <summary>
        ///A test for GetEnumerator ()
        ///</summary>
        [Test] // [TestMethod()]
        public void GetEnumeratorTest()
        {
            Stream inputStream = new MemoryStream(testData);
            int windowSize = 7;
            int blocksToBuffer = 3;

            SlidingStreamWindow target = new SlidingStreamWindow(inputStream, windowSize, blocksToBuffer);

            byte[] dummy = {1,2,3,4,5,6,7};
            IEnumerator<byte[]> actual = target.GetEnumerator();
            while(actual.MoveNext())
            {
                dummy = actual.Current;
            }
            Assert.AreEqual("lutions", Encoding.UTF8.GetString(dummy));
        }

        /// <summary>
        ///A test for GetNextWindow ()
        ///</summary>
//        [DeploymentItem("BlueLogic.SDelta.Core.dll")]
        [Test] // [TestMethod()]
        public void GetNextWindowTest()
        {
            Stream inputStream = new MemoryStream(testData);
            int windowSize = 7;
            int blocksToBuffer = 2;

            SlidingStreamWindow target = new SlidingStreamWindow(inputStream, windowSize, blocksToBuffer);

            BlueLogic_SDelta_Core_SlidingStreamWindowAccessor accessor = new BlueLogic_SDelta_Core_SlidingStreamWindowAccessor(target);

            byte[] expected = Encoding.UTF8.GetBytes("BlueLog");
            byte[] actual;

            actual = accessor.GetNextWindow();

            CollectionAssert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.SlidingStreamWindow.GetNextWindow did not return the expected value.");
        }

        /// <summary>
        ///A test for SkipRestOfWindow ()
        ///</summary>
        [Test] // [TestMethod()]
        public void SkipRestOfWindowTest()
        {
            Stream inputStream = new MemoryStream(testData); // TODO: Initialize to an appropriate value
            int windowSize = 25;
            int blocksToBuffer = 2;

            SlidingStreamWindow target = new SlidingStreamWindow(inputStream, windowSize, blocksToBuffer);
            BlueLogic_SDelta_Core_SlidingStreamWindowAccessor accessor = new BlueLogic_SDelta_Core_SlidingStreamWindowAccessor(target);

            accessor.GetNextWindow();
            target.SkipRestOfWindow();
            String result = Encoding.UTF8.GetString(accessor.GetNextWindow());
            String expected = "ons";

            Assert.AreEqual(expected, result, "SlidingStreamWindow.SkipRestOfWindow changed behaviour!");
        }

        /// <summary>
        ///A test for SlidingStreamWindow (Stream, int, int)
        ///</summary>
        [Test] // [TestMethod()]
        public void ConstructorTest()
        {
            Stream inputStream = new MemoryStream(testData);
            int windowSize = 7;
            int blocksToBuffer = 1;

            try
            {
                SlidingStreamWindow target = new SlidingStreamWindow(inputStream, windowSize, blocksToBuffer);
                Assert.Fail("SlidingStreamWindow was sucessfully initialized with blocksToBuffer < 2");
            }
            catch (ArgumentException ex)
            {
                Assert.AreNotEqual("", ex.Message);
            }
        }

    } // class
} // namespace
