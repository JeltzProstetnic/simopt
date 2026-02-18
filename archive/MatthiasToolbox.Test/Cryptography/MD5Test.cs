// complete

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using MatthiasToolbox.Cryptography.Hashes;

namespace MatthiasToolbox.Test.Cryptography
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.MD5 and is intended
    ///to contain all BlueLogic.SDelta.Core.MD5 Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class MD5Test
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
        ///A test for GetHash (byte[], byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void GetHashTest()
        {
            MD5 target = new MD5();

            byte[] data = { 123, 123, 123, 12, 3, 123, 12, 3, 12, 3 };
            byte[] iv = { 34, 67, 23, 7, 45, 12 };

            string expected = "x+hO4qAKaYFvJA0gINr4niJDFwctDA==";
            string actual;

            actual = target.GetHash(data, iv);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.MD5.GetHash did not return the expected value.");
        }

        /// <summary>
        ///A test for GetName ()
        ///</summary>
        [Test] // [TestMethod()]
        public void GetNameTest()
        {
            MD5 target = new MD5();

            string expected = "MD5";
            string actual;

            actual = target.Name;

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.MD5.GetName did not return the expected value.");
        }


        /// <summary>
        ///A test for GetHash (Stream, byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void GetHashTest1()
        {
            MD5 target = new MD5();

            byte[] bdata = { 123, 123, 123, 12, 3, 123, 12, 3, 12, 3 };
            Stream data = new MemoryStream(bdata);
            byte[] iv = { 34, 67, 23, 7, 45, 12 };

            string expected = "x+hO4qAKaYFvJA0gINr4niJDFwctDA==";
            string actual;

            actual = target.GetHash(data, iv);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.MD5.GetHash did not return the expected value.");
        }
    } // class
} // namespace