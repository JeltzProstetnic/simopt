// complete

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using MatthiasToolbox.Cryptography.Interfaces;
using MatthiasToolbox.Delta.Delta;
using MatthiasToolbox.Cryptography.Hashes;
using MatthiasToolbox.Cryptography.Checksums;
using MatthiasToolbox.Delta.Utilities;

namespace MatthiasToolbox.Test.Delta
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.TridgellDelta&lt;THash, TChecksum&gt; and is intended
    ///to contain all BlueLogic.SDelta.Core.TridgellDelta&lt;THash, TChecksum&gt; Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class TridgellDeltaTest
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
        ///A test for ApplyDelta (Stream, FileStream, FileStream, out string)
        ///</summary>
        [Test] // [TestMethod()]
        public void ApplyDeltaTest()
        {
            int blockSize = 5;
            IHashProvider<String> hash = new MD4();
            IRollingChecksumProvider<ulong> checksum = new Adler32(blockSize);
            TridgellDelta<String, ulong> target = new TridgellDelta<String, ulong>(blockSize, hash, checksum);

            //                                 1    2    3    4    5    6    7    8    9    0    1    2    3    4    5    6    7    8    9
            byte[] a = Encoding.UTF8.GetBytes("0123456789abcdefghijkAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBCCCCCCCCCCCCCCCCC0123456789abcdefghijk");

            //                                 iiiiiiiiiiiiiiiiiCC5CCCC5CCiiiiiiiiiiiiiiiiiiCC5CCCC5CCCC5CCCC5CCiiiC12CCC12CCC12CCiiCC0CCCC0CCCC0CCiiCC2CCCC3CCCC4CCi
            byte[] b = Encoding.UTF8.GetBytes("5678xcv9zzzzzzzzzAAAAAAAAAAöüäöüäöüäöüäöüäüäöAAAAAAAAAAAAAAAAAAAAAABCCCCCCCCCCCCCCCCC01234tt56789abcdefghijk");


            Stream dataA = new MemoryStream(a, false);
            Stream dataB = new MemoryStream(b, false);
            Stream NULL = new MemoryStream((int)dataA.Length);
            Stream rec = new MemoryStream((int)dataB.Length);
            Stream fs1 = new MemoryStream();
            BlockedHashset<String, ulong> bhs = target.GetBlockSignatures(dataA, NULL);

            target.GetDelta(dataB, bhs, ref fs1);

            fs1.Seek(0, SeekOrigin.Begin);
            String dig;
            target.ApplyDelta(fs1, dataA, rec, out dig);
            rec.Seek(0, SeekOrigin.Begin);
            byte[] brec = new byte[b.Length];
            rec.Read(brec, 0, b.Length);
            //String srec = Encoding.UTF8.GetString(brec);
            CollectionAssert.AreEqual(brec, b, "BlueLogic.SDelta.Core.TridgellDelta<THash, TChecksum>.GetDelta did not return the expected value.");

        }

        /// <summary>
        ///A test for GetBlockSignatures (Stream, Stream)
        ///</summary>
        [Test] // [TestMethod()]
        public void GetBlockSignaturesTest()
        {
            int blockSize = 5;
            IHashProvider<String> hash = new MD4();
            IRollingChecksumProvider<ulong> checksum = new Adler32(blockSize);
            TridgellDelta<String, ulong> target = new TridgellDelta<String, ulong>(blockSize, hash, checksum);

            byte[] b = Encoding.UTF8.GetBytes("AAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBCCCCCCCCCCCCCCCCC");

            Stream data = new MemoryStream(b, false);
            Stream NULL = new MemoryStream(5000);

            BlockedHashset<String, ulong> actual;

            actual = target.GetBlockSignatures(data, NULL);

            ulong u = 66191696;
            List<long> cp = (List<long>)actual.ChecksumPointers[u];
            long l = 8;
            Assert.AreEqual(l, cp[1]);
            Assert.AreEqual("123EF78AB11253A9094F053060E76AC2", actual.HashList[4]);
        }

        /// <summary>
        ///A test for ApplyDelta (Stream, Stream, Stream, out string)
        ///</summary>
        [Test] // [TestMethod()]
        public void ApplyDeltaTest1()
        {
            int blockSize = 5;
            TridgellDelta target = new TridgellDelta(blockSize);

            //                                 1    2    3    4    5    6    7    8    9    0    1    2    3    4    5    6    7    8    9
            byte[] a = Encoding.UTF8.GetBytes("0123456789abcdefghijkAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBCCCCCCCCCCCCCCCCC0123456789abcdefghijk");
            byte[] b = Encoding.UTF8.GetBytes("012345678xcv9abcdefghijkAAAAAAAAAAAAAABBBBBBBBBsdfsdfehjBBBBBBBBBCCCCCCCCCCCCCCCCC01234tt56789abcdefghijk");

            Stream dataA = new MemoryStream(a, false);
            Stream dataB = new MemoryStream(b, false);
            Stream NULL = new MemoryStream((int)dataA.Length);
            Stream rec = new MemoryStream((int)dataB.Length);
            
            Stream fs1 = new MemoryStream();
            BlockedHashset<String, ulong> bhs = target.GetBlockSignatures(dataA, NULL);

            target.GetDelta(dataB, bhs, ref fs1);

            fs1.Seek(0, SeekOrigin.Begin);
            String dig;
            target.ApplyDelta(fs1, dataA, rec, out dig);
            rec.Seek(0, SeekOrigin.Begin);
            byte[] brec = new byte[b.Length];
            rec.Read(brec, 0, b.Length);
            //String srec = Encoding.UTF8.GetString(brec);
            CollectionAssert.AreEqual(brec, b, "BlueLogic.SDelta.Core.TridgellDelta<THash, TChecksum>.GetDelta did not return the expected value.");
        }

        /// <summary>
        ///A test for GetBlockSignatures (Stream, Stream)
        ///</summary>
        [Test] // [TestMethod()]
        public void GetBlockSignaturesTest1()
        {
            int blockSize = 5;
            TridgellDelta target = new TridgellDelta(blockSize);

            byte[] b = Encoding.UTF8.GetBytes("AAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBCCCCCCCCCCCCCCCCC");

            Stream data = new MemoryStream(b, false);
            Stream NULL = new MemoryStream(5000);

            BlockedHashset<String, ulong> actual;

            actual = target.GetBlockSignatures(data, NULL);

            ulong u = 66191696;
            List<long> cp = (List<long>)actual.ChecksumPointers[u];
            long l = 8;
            Assert.AreEqual(l, cp[1]);
            Assert.AreEqual("123EF78AB11253A9094F053060E76AC2", actual.HashList[4]);
        }

        /// <summary>
        ///A test for GetDelta (Stream, BlockedHashset&lt;string,ulong&gt;, ref Stream)
        ///</summary>
        [Test] // [TestMethod()]
        public void GetDeltaTest()
        {
            int blockSize = 5;
            IHashProvider<String> hash = new MD4();
            IRollingChecksumProvider<ulong> checksum = new Adler32(blockSize);
            TridgellDelta<String, ulong> target = new TridgellDelta<String, ulong>(blockSize, hash, checksum);

            //                                 1    2    3    4    5    6    7    8    9    0    1    2    3    4    5    6    7    8    9
            byte[] a = Encoding.UTF8.GetBytes("0123456789abcdefghijkAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBCCCCCCCCCCCCCCCCC0123456789abcdefghijk");

            //                                 iiiiiiiiiiiiiiiiiCC5CCCC5CCiiiiiiiiiiiiiiiiiiCC5CCCC5CCCC5CCCC5CCiiiC12CCC12CCC12CCiiCC0CCCC0CCCC0CCiiCC2CCCC3CCCC4CCi
            byte[] b = Encoding.UTF8.GetBytes("5678xcv9zzzzzzzzzAAAAAAAAAAöüäöüäöüäöüäöüäüäöAAAAAAAAAAAAAAAAAAAAAABCCCCCCCCCCCCCCCCC01234tt56789abcdefghijk");


            Stream dataA = new MemoryStream(a, false);
            Stream dataB = new MemoryStream(b, false);
            Stream NULL = new MemoryStream((int)dataA.Length);
            Stream rec = new MemoryStream((int)dataB.Length);
            Stream fs1 = new MemoryStream();
            BlockedHashset<String, ulong> bhs = target.GetBlockSignatures(dataA, NULL);

            int c = target.GetDelta(dataB, bhs, ref fs1).Count;

            Assert.AreEqual(11, c, "BlueLogic.SDelta.Core.TridgellDelta<THash, TChecksum>.GetDelta did not return the expected value.");

        }

        /// <summary>
        ///A test for TridgellDelta ()
        ///</summary>
        [Test] // [TestMethod()]
        public void ConstructorTest()
        {
            TridgellDelta target = new TridgellDelta();
            Assert.IsNotNull(target);
        }

        /// <summary>
        ///A test for TridgellDelta (int)
        ///</summary>
        [Test] // [TestMethod()]
        public void ConstructorTest1()
        {
            int blockSize = 123;
            TridgellDelta target = new TridgellDelta(blockSize);
            Assert.IsNotNull(target);
        }

        /// <summary>
        ///A test for TridgellDelta (int, IDigestProvider)
        ///</summary>
        [Test] // [TestMethod()]
        public void ConstructorTest2()
        {
            int blockSize = 0;
            IDigestProvider digest = new SHA256();
            TridgellDelta target = new TridgellDelta(blockSize, digest);
            Assert.IsNotNull(target);
        }

        /// <summary>
        ///A test for TridgellDelta (int, IDigestProvider, IHashProvider&lt;THash&gt;, IChecksumProvider&lt;TChecksum&gt;)
        ///</summary>
        [Test] // [TestMethod()]
        public void ConstructorTest5()
        {
            int blockSize = 1;
            IDigestProvider digest = new SHA256();
            IHashProvider<String> hash = new MD4();
            IRollingChecksumProvider<ulong> checksum = new Adler32(1);

            TridgellDelta<String, ulong> target = new TridgellDelta<String, ulong>(blockSize, digest, hash, checksum);
            
            Assert.IsNotNull(target);
        }

        /// <summary>
        ///A test for TridgellDelta (int, IHashProvider&lt;THash&gt;, IChecksumProvider&lt;TChecksum&gt;)
        ///</summary>
        [Test] // [TestMethod()]
        public void ConstructorTest6()
        {
            int blockSize = 1;
            IHashProvider<String> hash = new MD4();
            IRollingChecksumProvider<ulong> checksum = new Adler32(1);

            TridgellDelta<String, ulong> target = new TridgellDelta<String, ulong>(blockSize, hash, checksum);
             
            Assert.IsNotNull(target);
        }
    }
}