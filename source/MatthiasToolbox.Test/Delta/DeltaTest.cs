// complete

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using MatthiasToolbox.Delta.Delta;

namespace MatthiasToolbox.Test.Delta
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.Delta&lt;TCommand&gt; and is intended
    ///to contain all BlueLogic.SDelta.Core.Delta&lt;TCommand&gt; Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class DeltaTest
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
        ///A test for BlockSize
        ///</summary>
        [Test] // [TestMethod()]
        public void BlockSizeTest()
        {
            Delta<String> target = new Delta<String>();
            int val = 12345;
            target.BlockSize = val;
            Assert.AreEqual(val, target.BlockSize, "BlueLogic.SDelta.Core.Delta<TCommand>.BlockSize was not set correctly.");
        }

        /// <summary>
        ///A test for CheckName
        ///</summary>
        [Test] // [TestMethod()]
        public void CheckNameTest()
        {
            Delta<String> target = new Delta<String>();
            string val = "TestName";
            target.CheckName = val;
            Assert.AreEqual(val, target.CheckName, "BlueLogic.SDelta.Core.Delta<TCommand>.CheckName was not set correctly.");
        }

        /// <summary>
        ///A test for Delta ()
        ///</summary>
        [Test] // [TestMethod()]
        public void ConstructorTest()
        {
            Delta<String> target = new Delta<String>();
            Assert.IsNotNull(target.Commands);
        }

        /// <summary>
        ///A test for Delta (int, List&lt;TCommand&gt;, string, string, string, string)
        ///</summary>
        [Test] // [TestMethod()]
        public void ConstructorTest1()
        {
            int blockSize = 1234;
            List<String> commands = new List<string>();
            commands.Add("test");
            string digestName = "digest";
            string hashName = "hash";
            string checkName = "check";
            string digest = "theDigest";

            Delta<String> target = new Delta<String>(blockSize, commands, digestName, hashName, checkName, digest);

            Assert.AreEqual(blockSize, target.BlockSize);
            Assert.AreEqual(commands, target.Commands);
            Assert.AreEqual(digestName, target.DigestName);
            Assert.AreEqual(hashName, target.HashName);
            Assert.AreEqual(checkName, target.CheckName);
            Assert.AreEqual(digest, target.Digest);
        }

        /// <summary>
        ///A test for Digest
        ///</summary>
        [Test] // [TestMethod()]
        public void DigestTest()
        {
            Delta<String> target = new Delta<String>();
            string val = "test";
            target.Digest = val;

            Assert.AreEqual(val, target.Digest, "BlueLogic.SDelta.Core.Delta<TCommand>.Digest was not set correctly.");
        }

        /// <summary>
        ///A test for DigestName
        ///</summary>
        [Test] // [TestMethod()]
        public void DigestNameTest()
        {
            Delta<String> target = new Delta<String>();
            string val = "test";
            target.DigestName = val;

            Assert.AreEqual(val, target.DigestName, "BlueLogic.SDelta.Core.Delta<TCommand>.DigestName was not set correctly.");
        }

        /// <summary>
        ///A test for HashName
        ///</summary>
        [Test] // [TestMethod()]
        public void HashNameTest()
        {
            Delta<String> target = new Delta<String>();
            string val = "test";
            target.HashName = val;

            Assert.AreEqual(val, target.HashName, "BlueLogic.SDelta.Core.Delta<TCommand>.HashName was not set correctly.");
        }
    
    } // class
} // namespace