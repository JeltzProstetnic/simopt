// complete

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using MatthiasToolbox.Cryptography.Hashes;

namespace MatthiasToolbox.Test.Cryptography
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.Cryptography.SHA512 and is intended
    ///to contain all BlueLogic.SDelta.Core.Cryptography.SHA512 Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class SHA512Test
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
            SHA512 target = new SHA512();

            byte[] data = { 123, 123, 123, 12, 3, 123, 12, 3, 12, 3 };
            byte[] iv = { 34, 67, 23, 7, 45, 12 };

            string expected = "batY0vhaSACgSkzJEyGkDljuCsZ9XFYDgYiDhhLsZ5mFOzbrKPbfWNpJpVdqpTZ6d/7wBxeHFUN23bi8New5iCJDFwctDA==";
            string actual;

            actual = target.GetHash(data, iv);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.Cryptography.SHA512.GetHash did not return the expected val" +
                    "ue.");
        }

        /// <summary>
        ///A test for GetHash (Stream, byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void GetHashTest1()
        {
            SHA512 target = new SHA512();

            byte[] bdata = { 123, 123, 123, 12, 3, 123, 12, 3, 12, 3 };
            Stream data = new MemoryStream(bdata);
            byte[] iv = { 34, 67, 23, 7, 45, 12 };

            string expected = "batY0vhaSACgSkzJEyGkDljuCsZ9XFYDgYiDhhLsZ5mFOzbrKPbfWNpJpVdqpTZ6d/7wBxeHFUN23bi8New5iCJDFwctDA==";
            string actual;

            actual = target.GetHash(data, iv);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.Cryptography.SHA512.GetHash did not return the expected val" +
                    "ue.");
        }

        /// <summary>
        ///A test for Name
        ///</summary>
        [Test] // [TestMethod()]
        public void NameTest()
        {
            SHA512 target = new SHA512();
            string val = "SHA512";
            Assert.AreEqual(val, target.Name, "BlueLogic.SDelta.Core.Cryptography.SHA512.Name was not set correctly.");
        }
    }
}