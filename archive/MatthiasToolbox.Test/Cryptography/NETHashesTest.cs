// complete

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.IO;
using MatthiasToolbox.Cryptography.Utilities;

namespace MatthiasToolbox.Test.Cryptography
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.NetHashes and is intended
    ///to contain all BlueLogic.SDelta.Core.NetHashes Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class NETHashesTest
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
        ///A test for ComputeHash (byte[], string, byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void ComputeHashTest()
        {
            byte[] plainText = Encoding.UTF8.GetBytes("BlueLogic Software Solutions");
            string hashAlgorithm = "MD5";

            byte[] saltBytes = {1,2,3,4,5,6};

            string expected = "b3oyMRCKGeuE8czUCKgDFgECAwQFBg==";
            string actual;

            actual = NetHashes.ComputeHash(plainText, hashAlgorithm, saltBytes);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.NetHashes.ComputeHash did not return the expected value.");
        }

        /// <summary>
        ///A test for ComputeHash (Stream, string, byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void ComputeHashTest1()
        {
            byte[] bPlainText = Encoding.UTF8.GetBytes("BlueLogic Software Solutions");
            Stream plainText = new MemoryStream(bPlainText);

            string hashAlgorithm = "MD5";
            byte[] saltBytes = { 1, 2, 3, 4, 5, 6 };

            string expected = "b3oyMRCKGeuE8czUCKgDFgECAwQFBg==";
            string actual;

            actual = NetHashes.ComputeHash(plainText, hashAlgorithm, saltBytes);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.NetHashes.ComputeHash did not return the expected value.");
        }

        /// <summary>
        ///A test for ComputeHash (string, string, byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void ComputeHashTest2()
        {
            string plainText = "BlueLogic Software Solutions";
            string hashAlgorithm = "MD5";

            byte[] saltBytes = {1,2,3,4,5,6};

            string expected = "b3oyMRCKGeuE8czUCKgDFgECAwQFBg==";
            string actual;

            actual = NetHashes.ComputeHash(plainText, hashAlgorithm, saltBytes);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.NetHashes.ComputeHash did not return the expected value.");
        }

        /// <summary>
        ///A test for VerifyHash (string, string, string)
        ///</summary>
        [Test] // [TestMethod()]
        public void VerifyHashTest()
        {
            string plainText = "BlueLogic Software Solutions";
            string hashAlgorithm = "MD5";

            string hashValue = "b3oyMRCKGeuE8czUCKgDFgECAwQFBg==";

            bool expected = true;
            bool actual;

            actual = NetHashes.VerifyHash(plainText, hashAlgorithm, hashValue);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.NetHashes.VerifyHash did not return the expected value.");
        }

    } // class
} // namespace
