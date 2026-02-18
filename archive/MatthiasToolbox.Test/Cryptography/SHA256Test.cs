//  complete

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.IO;
using MatthiasToolbox.Cryptography.Hashes;

namespace MatthiasToolbox.Test.Cryptography
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.SHA256 and is intended
    ///to contain all BlueLogic.SDelta.Core.SHA256 Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class SHA256Test
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
            SHA256 target = new SHA256();

            byte[] data = Encoding.UTF8.GetBytes("BlueLogic Software Solutions");
            byte[] iv = {123, 45, 67, 89, 234, 56, 78};

            string expected = "pZ698BT8pmm+v9tdLbzl1hOk/Yq1mHfT/TPXNJivYO57LUNZ6jhO";
            string actual;

            actual = target.GetHash(data, iv);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.SHA256.GetHash did not return the expected value.");
        }

        /// <summary>
        ///A test for GetHash (Stream, byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void GetHashTest1()
        {
            SHA256 target = new SHA256();

            byte[] bData = Encoding.UTF8.GetBytes("BlueLogic Software Solutions");
            Stream data = new MemoryStream(bData);
            
            byte[] iv = { 123, 45, 67, 89, 234, 56, 78 };

            string expected = "pZ698BT8pmm+v9tdLbzl1hOk/Yq1mHfT/TPXNJivYO57LUNZ6jhO";
            string actual;

            actual = target.GetHash(data, iv);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.SHA256.GetHash did not return the expected value.");
        }

        /// <summary>
        ///A test for GetName ()
        ///</summary>
        [Test] // [TestMethod()]
        public void GetNameTest()
        {
            SHA256 target = new SHA256();

            string expected = "SHA256";
            string actual;

            actual = target.Name;

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.SHA256.GetName did not return the expected value.");
        }

    } // class
} // namespace