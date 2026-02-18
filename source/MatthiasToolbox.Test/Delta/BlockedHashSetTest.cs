// complete

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using MatthiasToolbox.Delta.Utilities;
using MatthiasToolbox.Cryptography.Interfaces;
using MatthiasToolbox.Cryptography.Hashes;
using MatthiasToolbox.Cryptography.Checksums;

namespace MatthiasToolbox.Test.Delta
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.BlockedHashset&lt;THash, TChecksum&gt; and is intended
    ///to contain all BlueLogic.SDelta.Core.BlockedHashset&lt;THash, TChecksum&gt; Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class BlockedHashsetTest
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
        ///A test for AddPair (THash, TChecksum)
        ///</summary>
        [Test] // [TestMethod()]
        public void AddPairTest()
        {
            BlockedHashset<String, int> target = new BlockedHashset<String, int>();
            String strongHash = "StrongHash§$%&/()\\";
            int rollingHash = int.MaxValue;

            long expected = 0;
            long actual = target.AddPair(strongHash, rollingHash);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.BlockedHashset<THash, TChecksum>.AddPair test 1 did not return the" +
                    " expected value.");

            expected = 1;
            actual = target.AddPair(strongHash, rollingHash);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.BlockedHashset<THash, TChecksum>.AddPair test 2 did not return the" +
                    " expected value.");
        }

        /// <summary>
        ///A test for BlockedHashset ()
        ///</summary>
        [Test] // [TestMethod()]
        public void ConstructorTest()
        {
            BlockedHashset<int, String> target = new BlockedHashset<int, String>();

            Assert.AreEqual(0, target.BlockSize);
            Assert.AreEqual("", target.HashName);
            Assert.AreEqual("", target.CheckName);
            Assert.IsNotNull(target.ChecksumPointers);
            Assert.IsNotNull(target.HashList);
        }

        /// <summary>
        ///A test for BlockedHashset (int, string, string)
        ///</summary>
        [Test] // [TestMethod()]
        public void ConstructorTest1()
        {
            BlockedHashset<int, String> target = new BlockedHashset<int, String>(123, "HashName123", "CheckName123");

            Assert.AreEqual(123, target.BlockSize);
            Assert.AreEqual("HashName123", target.HashName);
            Assert.AreEqual("CheckName123", target.CheckName);
            Assert.IsNotNull(target.ChecksumPointers);
            Assert.IsNotNull(target.HashList);
        }

        /// <summary>
        ///A test for BlockSize
        ///</summary>
        [Test] // [TestMethod()]
        public void BlockSizeTest()
        {
            BlockedHashset<String, int> target = new BlockedHashset<String, int>();

            int val = 123;
            target.BlockSize = val;

            Assert.AreEqual(val, target.BlockSize, "BlueLogic.SDelta.Core.BlockedHashset<THash, TChecksum>.BlockSize was not set corr" +
                   "ectly.");
        }

        /// <summary>
        ///A test for Check (byte[], byte[], IHashProvider&lt;THash&gt;, IChecksumProvider&lt;TChecksum&gt;)
        ///</summary>
        [Test] // [TestMethod()]
        public void CheckTest()
        {
            BlockedHashset<String, ulong> target = new BlockedHashset<String, ulong>(123, "HashName123", "CheckName123");

            byte[] iv = { 0x01, 0xA2, 0x09, 0x66 };
            byte[] data1 = { 0x01, 0x02, 0x02, 0x02 };
            byte[] data2 = { 0x02, 0x02, 0x02, 0x01 };
            byte[] data3 = { 0x02, 0x01, 0x02, 0x01 };

            IHashProvider<String> md4 = new MD4();
            IRollingChecksumProvider<ulong> adler = new Adler32(4);

            target.AddPair(md4.GetHash(data1, iv), adler.GetChecksum(data1));
            target.AddPair(md4.GetHash(data2, iv), adler.GetChecksum(data2));

            long expected1 = 0;
            long expected2 = 1;
            long expected3 = -1;

            ulong r1 = 1;
            ulong r2 = 1;
            ulong r3 = 1;
            byte b1 = 0;

            long result1 = target.Check(data1, iv, md4, adler, ref r1, ref b1);
            long result2 = target.Check(data2, iv, md4, adler, ref r2, ref b1);
            long result3 = target.Check(data3, iv, md4, adler, ref r3, ref b1);

            Assert.AreEqual(expected1, result1, "BlueLogic.SDelta.Core.BlockedHashset<THash, TChecksum>.Check test 1 did not return the e" +
                    "xpected value.");
            Assert.AreEqual(expected2, result2, "BlueLogic.SDelta.Core.BlockedHashset<THash, TChecksum>.Check test 2 did not return the e" +
                    "xpected value.");
            Assert.AreEqual(expected3, result3, "BlueLogic.SDelta.Core.BlockedHashset<THash, TChecksum>.Check test 3 did not return the e" +
                    "xpected value.");
        }

        /// <summary>
        ///A test for CheckName
        ///</summary>
        [Test] // [TestMethod()]
        public void CheckNameTest()
        {
            BlockedHashset<String, int> target = new BlockedHashset<String, int>();

            string val = "CheckName123"; // TODO: Assign to an appropriate value for the property
            target.CheckName = val;

            Assert.AreEqual(val, target.CheckName, "BlueLogic.SDelta.Core.BlockedHashset<THash, TChecksum>.CheckName was not set corr" +
                   "ectly.");
        }

        /// <summary>
        ///A test for ChecksumPointers
        ///</summary>
        [Test] // [TestMethod()]
        public void ChecksumPointersTest()
        {
            BlockedHashset<String, ulong> target = new BlockedHashset<String, ulong>(123, "HashName123", "CheckName123");

            byte[] data1 = { 0x01, 0x02, 0x02, 0x02 };
            byte[] data2 = { 0x02, 0x02, 0x02, 0x01 };

            MD4 md4 = new MD4();
            Adler32 adler = new Adler32(4);

            target.AddPair(md4.GetHexHashFromBytes(data1), adler.GetChecksum(data1));
            target.AddPair(md4.GetHexHashFromBytes(data2), adler.GetChecksum(data1));

            Assert.AreEqual(1, target.ChecksumPointers.Count);
        }

        /// <summary>
        ///A test for HashList
        ///</summary>
        [Test] // [TestMethod()]
        public void HashListTest()
        {
            BlockedHashset<String, ulong> target = new BlockedHashset<String, ulong>(123, "HashName123", "CheckName123");

            SortedList<long, String> val = new SortedList<long, string>();

            byte[] data1 = { 0x01, 0x02, 0x02, 0x02 };
            byte[] data2 = { 0x02, 0x02, 0x02, 0x01 };

            MD4 md4 = new MD4();
            Adler32 adler = new Adler32(4);

            target.AddPair(md4.GetHexHashFromBytes(data1), adler.GetChecksum(data1));
            target.AddPair(md4.GetHexHashFromBytes(data2), adler.GetChecksum(data1));

            Assert.AreEqual(2, target.HashList.Count);
        }

        /// <summary>
        ///A test for HashName
        ///</summary>
        [Test] // [TestMethod()]
        public void HashNameTest()
        {
            BlockedHashset<String, int> target = new BlockedHashset<String, int>();

            string val = "EinTest123";
            target.HashName = val;

            Assert.AreEqual(val, target.HashName, "BlueLogic.SDelta.Core.BlockedHashset<THash, TChecksum>.HashName was not set corre" +
                    "ctly.");
        }

        /// <summary>
        ///A test for Check (byte[], byte[], IHashProvider&lt;THash&gt;, IChecksumProvider&lt;TChecksum&gt;, ref TChecksum, ref byte)
        ///</summary>
        [Test] // [TestMethod()]
        public void CheckTest1()
        {
            BlockedHashset<String, ulong> target = new BlockedHashset<String, ulong>();

            byte[] data = {34,76,5,23};
            byte[] iv = {65,23,6,23,6,8};
            IHashProvider<String> hashProvider = new MD4();
            IRollingChecksumProvider<ulong> checksumProvider = new Adler32(1);

            ulong lastChecksum = 1;
            byte old = 0;
            long expected = -1;
            
            long actual = target.Check(data, iv, hashProvider, checksumProvider, ref lastChecksum, ref old);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.BinaryDelta.BlockedHashset<THash, TChecksum>.Check did not " +
                   "return the expected value.");
        }
    } // class
} // namespace