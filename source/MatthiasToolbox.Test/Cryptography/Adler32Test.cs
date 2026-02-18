// complete

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using MatthiasToolbox.Cryptography.Checksums;

namespace MatthiasToolbox.Test.Cryptography
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.Adler32 and is intended
    ///to contain all BlueLogic.SDelta.Core.Adler32 Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class Adler32Test
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
        ///A test for adler32 (ulong, byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void GetChecksumTest()
        {
            Adler32 target = new Adler32(4);
            byte[] buf = { 0xF1, 0xF2, 0xF3, 0xF4 };

            ulong expected = 158860235;
            ulong actual;

            actual = target.GetChecksum(buf);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");
        }

        /// <summary>
        ///A test for GetChecksum (byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void GetChecksumTest1()
        {
            Adler32 target = new Adler32(4);

            byte[] data = { 0xF1, 0xF3, 0xF2, 0xF4 };

            ulong expected = 158925771;
            ulong actual;

            actual = target.GetChecksum(data);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.Adler32.GetChecksum did not return the expected value.");
        }

        /// <summary>
        ///A test for adler32 (ulong, byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void GetChecksumTest2()
        {
            Adler32 target = new Adler32(4);
            byte[] buf0 = { 11, 22, 33, 44 };
            byte[] buf1 =     { 22, 33, 44, 5 };
            byte[] buf2 =         { 33, 44, 5, 7 };
            byte[] buf3 =             { 44, 5, 7, 9 };
            byte[] buf4 =                 { 5, 7, 9, 11 };


            ulong adler0 = 14680175; // 111 / 224
            ulong adler1 = 18612329; // 
            ulong adler2 = 18677850; // 
            ulong adler3 = 14286914; // 
            ulong adler4 = 4849697;  //  33 / 74

            ulong actual = target.GetChecksum(buf0);
            Assert.AreEqual(adler0, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf1);
            Assert.AreEqual(adler1, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf2);
            Assert.AreEqual(adler2, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf3);
            Assert.AreEqual(adler3, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf4);
            Assert.AreEqual(adler4, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a1 = target.GetChecksum(adler0, 11, 5);
            Assert.AreEqual(adler1, a1, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a2 = target.GetChecksum(a1, 22, 7);
            Assert.AreEqual(adler2, a2, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a3 = target.GetChecksum(a2, 33, 9);
            Assert.AreEqual(adler3, a3, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a4 = target.GetChecksum(a3, 44, 11);
            Assert.AreEqual(adler4, a4, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");
        }

        /// <summary>
        ///A test for adler32 (ulong, byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void GetChecksumTest3()
        {
            Adler32 target = new Adler32(4);

            byte[] buf0 = { 0, 0, 0, 0 };
            byte[] buf1 =     { 0, 0, 0, 255 };
            byte[] buf2 =         { 0, 0, 255, 255 };
            byte[] buf3 =             { 0, 255, 255, 255 };
            byte[] buf4 =                 { 255, 255, 255, 255 };

            ulong adler0 = 262145;
            ulong adler1 = 16974080;
            ulong adler2 = 50397695;
            ulong adler3 = 100532990;
            ulong adler4 = 167379965;

            ulong actual = target.GetChecksum(buf0);
            Assert.AreEqual(adler0, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf1);
            Assert.AreEqual(adler1, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf2);
            Assert.AreEqual(adler2, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf3);
            Assert.AreEqual(adler3, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf4);
            Assert.AreEqual(adler4, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a1 = target.GetChecksum(adler0, 0, 255);
            Assert.AreEqual(adler1, a1, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a2 = target.GetChecksum(a1, 0, 255);
            Assert.AreEqual(adler2, a2, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a3 = target.GetChecksum(a2, 0, 255);
            Assert.AreEqual(adler3, a3, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a4 = target.GetChecksum(a3, 0, 255);
            Assert.AreEqual(adler4, a4, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");
        }

        /// <summary>
        ///A test for adler32 (ulong, byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void GetChecksumTest4()
        {
            Adler32 target = new Adler32(4);

            byte[] buf0 = { 255, 255, 255, 255 };
            byte[] buf1 =     { 255, 255, 255, 0 };
            byte[] buf2 =         { 255, 255, 0, 0 };
            byte[] buf3 =             { 255, 0, 0, 0 };
            byte[] buf4 =                 { 0, 0, 0, 0 };

            ulong adler0 = 167379965;
            ulong adler1 = 150668030;
            ulong adler2 = 117244415;
            ulong adler3 = 67109120;
            ulong adler4 = 262145;

            ulong actual = target.GetChecksum(buf0);
            Assert.AreEqual(adler0, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf1);
            Assert.AreEqual(adler1, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf2);
            Assert.AreEqual(adler2, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf3);
            Assert.AreEqual(adler3, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf4);
            Assert.AreEqual(adler4, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a1 = target.GetChecksum(adler0, 255, 0);
            Assert.AreEqual(adler1, a1, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a2 = target.GetChecksum(a1, 255, 0);
            Assert.AreEqual(adler2, a2, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a3 = target.GetChecksum(a2, 255, 0);
            Assert.AreEqual(adler3, a3, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a4 = target.GetChecksum(a3, 255, 0);
            Assert.AreEqual(adler4, a4, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");
        }

        /// <summary>
        ///A test for adler32 (ulong, byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void GetChecksumTest5()
        {
            Adler32 target = new Adler32(4);

            byte[] buf0 = { 85, 7, 27, 2 };
            byte[] buf1 =     { 7, 27, 2, 88 };
            byte[] buf2 =         { 27, 2, 88, 7 };
            byte[] buf3 =             { 2, 88, 7, 98 };
            byte[] buf4 =                 { 88, 7, 98, 9 };

            ulong adler0 = 27590778;
            ulong adler1 = 13435005;
            ulong adler2 = 19726461;
            ulong adler3 = 25428164;
            ulong adler4 = 38142155;

            ulong actual = target.GetChecksum(buf0);
            Assert.AreEqual(adler0, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf1);
            Assert.AreEqual(adler1, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf2);
            Assert.AreEqual(adler2, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf3);
            Assert.AreEqual(adler3, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf4);
            Assert.AreEqual(adler4, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a1 = target.GetChecksum(adler0, 85, 88);
            Assert.AreEqual(adler1, a1, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a2 = target.GetChecksum(a1, 7, 7);
            Assert.AreEqual(adler2, a2, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a3 = target.GetChecksum(a2, 27, 98);
            Assert.AreEqual(adler3, a3, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a4 = target.GetChecksum(a3, 2, 9);
            Assert.AreEqual(adler4, a4, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");
        }

        /// <summary>
        ///A test for adler32 (ulong, byte[])
        ///</summary>
        [Test] // [TestMethod()]
        public void GetChecksumTest6()
        {
            Adler32 target = new Adler32(7);

            byte[] buf0 = { 85, 7, 27, 2, 88, 7, 98 };
            byte[] buf1 =     { 7, 27, 2, 88, 7, 98, 9 };
            byte[] buf2 =         { 27, 2, 88, 7, 98, 9, 72 };
            byte[] buf3 =             { 2, 88, 7, 98, 9, 72, 4 };
            byte[] buf4 =                 { 88, 7, 98, 9, 72, 4, 178 };
            byte[] buf5 =                     { 7, 98, 9, 72, 4, 178, 97 };
            byte[] buf6 =                         { 98, 9, 72, 4, 178, 97, 2 };
            byte[] buf7 =                             { 9, 72, 4, 178, 97, 2, 95 };
            byte[] buf8 =                                 { 72, 4, 178, 97, 2, 95, 41 };

            ulong adler0 = 76218683;
            ulong adler1 = 52822255;
            ulong adler2 = 69468464;
            ulong adler3 = 75432217;
            ulong adler4 = 104399305;
            ulong adler5 = 94503378;
            ulong adler6 = 121438669;
            ulong adler7 = 106430922;
            ulong adler8 = 134349290;

            ulong actual = target.GetChecksum(buf0);
            Assert.AreEqual(adler0, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf1);
            Assert.AreEqual(adler1, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf2);
            Assert.AreEqual(adler2, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf3);
            Assert.AreEqual(adler3, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf4);
            Assert.AreEqual(adler4, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf5);
            Assert.AreEqual(adler5, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf6);
            Assert.AreEqual(adler6, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf7);
            Assert.AreEqual(adler7, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            actual = target.GetChecksum(buf8);
            Assert.AreEqual(adler8, actual, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a1 = target.GetChecksum(adler0, 85, 9);
            Assert.AreEqual(adler1, a1, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a2 = target.GetChecksum(a1, 7, 72);
            Assert.AreEqual(adler2, a2, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a3 = target.GetChecksum(a2, 27, 4);
            Assert.AreEqual(adler3, a3, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a4 = target.GetChecksum(a3, 2, 178);
            Assert.AreEqual(adler4, a4, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a5 = target.GetChecksum(a4, 88, 97);
            Assert.AreEqual(adler5, a5, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a6 = target.GetChecksum(a5, 7, 2);
            Assert.AreEqual(adler6, a6, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a7 = target.GetChecksum(a6, 98, 95);
            Assert.AreEqual(adler7, a7, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");

            ulong a8 = target.GetChecksum(a7, 9, 41);
            Assert.AreEqual(adler8, a8, "BlueLogic.SDelta.Core.Adler32.adler32 did not return the expected value.");
        }

        /// <summary>
        ///A test for GetName ()
        ///</summary>
        [Test] // [TestMethod()]
        public void GetNameTest()
        {
            Adler32 target = new Adler32(1);

            string expected = "Adler32";
            string actual;

            actual = target.Name;

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.Adler32.GetName did not return the expected value.");
        }


        /// <summary>
        ///A test for adler32 (ulong, byte[])
        ///</summary>
        // [DeploymentItem("BlueLogic.SDelta.Core.dll")]
        [Test] // [TestMethod()]
        public void adler32Test()
        {
            ulong adler = 1;

            byte[] buf = {12, 13, 14};

            ulong expected = 5177384;
            ulong actual;

            actual = BlueLogic_SDelta_Core_Cryptography_Adler32Accessor.adler32(adler, buf);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.Cryptography.Adler32.adler32 did not return the expected value.");
        }

        /// <summary>
        ///A test for BlockSize
        ///</summary>
        [Test] // [TestMethod()]
        public void BlockSizeTest()
        {
            int blockSize = 0;
            Adler32 target = new Adler32(blockSize);
            int val = 0;
            target.BlockSize = val;

            Assert.AreEqual(val, target.BlockSize, "BlueLogic.SDelta.Core.Cryptography.Adler32.BlockSize was not set correctly.");
        }
    } // class
} // namespace
