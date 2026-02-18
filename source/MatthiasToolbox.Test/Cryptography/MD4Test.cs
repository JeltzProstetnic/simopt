// complete

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using MatthiasToolbox.Cryptography.Hashes;

namespace MatthiasToolbox.Test.Cryptography
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.MD4 and is intended
    ///to contain all BlueLogic.SDelta.Core.MD4 Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class MD4Test
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
        ///A test for BytesToHex (byte[], int)
        ///</summary>
        [Test] // [TestMethod()]
        public void bytesToHexTest()
        {
            byte[] a = {1,2,3,4};
            int len = 4;

            string expected = "01020304";
            string actual;

            actual = MD4.BytesToHex(a, len);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.MD4.BytesToHex did not return the expected value.");
        }

        /// <summary>
        ///A test for engineDigest ()
        ///</summary>
//        [DeploymentItem("BlueLogic.SDelta.Core.dll")]
        [Test] // [TestMethod()]
        public void engineDigestTest()
        {
            MD4 target = new MD4();

            BlueLogic_SDelta_Core_MD4Accessor accessor = new BlueLogic_SDelta_Core_MD4Accessor(target);

            byte[] expected = {49, 214, 207, 224, 209, 106, 233, 49, 183, 60, 89, 215, 224, 192, 137, 192};
            byte[] actual;

            actual = accessor.engineDigest();

            CollectionAssert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.MD4.engineDigest did not return the expected value.");
        }

        /// <summary>
        ///A test for GetByteHashFromByte (byte)
        ///</summary>
        [Test] // [TestMethod()]
        public void GetByteHashFromByteTest()
        {
            MD4 target = new MD4();
            byte b = 123;

            byte[] expected = {24, 199, 146, 133, 181, 20, 198, 63, 201, 3, 94, 63, 146, 10, 228, 119};
            byte[] actual;

            actual = target.GetByteHashFromByte(b);

            CollectionAssert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.MD4.GetByteHashFromByte did not return the expected value.");
        }

        /// <summary>
        ///A test for GetByteHashFromBytes (byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void GetByteHashFromBytesTest()
        {
            MD4 target = new MD4();

            byte[] b = {1,235,67,65,3,34,5,5,47};

            byte[] expected = {202, 5, 121, 75, 213, 174, 165, 139, 37, 43, 216, 237, 161, 203, 239, 45};
            byte[] actual;

            actual = target.GetByteHashFromBytes(b);

            CollectionAssert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.MD4.GetByteHashFromBytes did not return the expected value.");
        }

        /// <summary>
        ///A test for GetByteHashFromString (string)
        ///</summary>
        [Test] // [TestMethod()]
        public void GetByteHashFromStringTest()
        {
            MD4 target = new MD4();
            string s = "BlueLogic Software Solutions";

            byte[] expected = {181, 29, 64, 59, 29, 86, 211, 128, 63, 245, 206, 171, 80, 95, 152, 45};
            byte[] actual;

            actual = target.GetByteHashFromString(s);

            CollectionAssert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.MD4.GetByteHashFromString did not return the expected value.");
        }

        /// <summary>
        ///A test for GetHash (byte[], byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void GetHashTest()
        {
            MD4 target = new MD4();

            byte[] data = {1,234,5,12,4,124,51,24,12,45,23,45,213,45,245,214,3};

            byte[] iv = {1,3,6,8,90,23,2};

            string expected = "B9674DC5C43F8BE92B5D5196ABA19211";
            string actual;

            actual = target.GetHash(data, iv);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.MD4.GetHash did not return the expected value.");
        }

        /// <summary>
        ///A test for GetHexHashFromByte (byte)
        ///</summary>
        [Test] // [TestMethod()]
        public void GetHexHashFromByteTest()
        {
            MD4 target = new MD4();

            byte b = 42;

            string expected = "E06270E574544E1902B2CD495AD2D78D";
            string actual;

            actual = target.GetHexHashFromByte(b);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.MD4.GetHexHashFromByte did not return the expected value.");
        }

        /// <summary>
        ///A test for GetHexHashFromBytes (byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void GetHexHashFromBytesTest()
        {
            MD4 target = new MD4();

            byte[] b = {23,12,4,4,23,3,4,123,46};

            string expected = "9437B3DF859CECEED7C73E68053429E7";
            string actual;

            actual = target.GetHexHashFromBytes(b);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.MD4.GetHexHashFromBytes did not return the expected value.");
        }

        /// <summary>
        ///A test for GetHexHashFromString (string)
        ///</summary>
        [Test] // [TestMethod()]
        public void GetHexHashFromStringTest()
        {
            MD4 target = new MD4();

            string s = "BlueLogic Software Solutions";

            string expected = "B51D403B1D56D3803FF5CEAB505F982D";
            string actual;

            actual = target.GetHexHashFromString(s);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.MD4.GetHexHashFromString did not return the expected value.");
        }

        /// <summary>
        ///A test for GetName ()
        ///</summary>
        [Test] // [TestMethod()]
        public void GetNameTest()
        {
            MD4 target = new MD4();

            string expected = "MD4";
            string actual;

            actual = target.Name;

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.MD4.GetName did not return the expected value.");
        }

    } // class
} // namespace