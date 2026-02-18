// 

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;

namespace MatthiasToolbox.Test.Delta
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.XmlSerializationProvider&lt;THash, TCheck&gt; and is intended
    ///to contain all BlueLogic.SDelta.Core.XmlSerializationProvider&lt;THash, TCheck&gt; Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class XmlSerializationProviderTest
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
        ///A test for ReadBlockedHashSet (ref BlockedHashset&lt;THash,TCheck&gt;, Stream)
        ///</summary>
        [Test] // [TestMethod()]
        public void ReadBlockedHashSetTest()
        {
            //XmlSerializationProvider<String, String> target = new XmlSerializationProvider<String, String>();

            //BlockedHashset<String, String> hashset = null; // TODO: MemoryStream?
            //BlockedHashset<String, String> hashset_expected = null;

            //Stream file = null;

            //bool expected = false;
            //bool actual;

            //actual = target.ReadBlockedHashSet(ref hashset, file);

            //Assert.AreEqual(hashset_expected, hashset, "hashset_ReadBlockedHashSet_expected was not set correctly.");
            //Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.XmlSerializationProvider<THash, TCheck>.ReadBlockedHashSet " +
            //       "did not return the expected value.");
        }

        /// <summary>
        ///A test for ReadDelta (ref Delta&lt;BinaryCommand&gt;, Stream)
        ///</summary>
        [Test] // [TestMethod()]
        public void ReadDeltaTest()
        {
            // XmlSerializationProvider<THash, TCheck> target = new XmlSerializationProvider<THash, TCheck>();
            // 
            // BlueLogic.SDelta.Core.Delta<BlueLogic.SDelta.Core.BinaryCommand> delta = null;
            // BlueLogic.SDelta.Core.Delta<BlueLogic.SDelta.Core.BinaryCommand> delta_expected = null;
            // 
            // Stream file = null;
            // 
            // bool expected = false;
            // bool actual;
            // 
            // actual = target.ReadDelta(ref delta, file);
            // 
            // Assert.AreEqual(delta_expected, delta, "delta_ReadDelta_expected was not set correctly.");
            // Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.XmlSerializationProvider<THash, TCheck>.ReadDelta did not r" +
            //        "eturn the expected value.");
            // Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for WriteBlockedHashSet (BlockedHashset&lt;THash,TCheck&gt;, Stream)
        ///</summary>
        [Test] // [TestMethod()]
        public void WriteBlockedHashSetTest()
        {
            // XmlSerializationProvider<THash, TCheck> target = new XmlSerializationProvider<THash, TCheck>();
            // 
            // BlueLogic.SDelta.Core.BlockedHashset<THash, TCheck> hashset = null;
            // 
            // Stream file = null; 
            // 
            // bool expected = false;
            // bool actual;
            // 
            // actual = target.WriteBlockedHashSet(hashset, file);
            // 
            // Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.XmlSerializationProvider<THash, TCheck>.WriteBlockedHashSet" +
            //        " did not return the expected value.");
            // Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for WriteDelta (Delta&lt;BinaryCommand&gt;, Stream)
        ///</summary>
        [Test] // [TestMethod()]
        public void WriteDeltaTest()
        {
            // XmlSerializationProvider<THash, TCheck> target = new XmlSerializationProvider<THash, TCheck>();
            // 
            // BlueLogic.SDelta.Core.Delta<BlueLogic.SDelta.Core.BinaryCommand> delta = null;
            // 
            // Stream file = null;
            // 
            // bool expected = false;
            // bool actual;
            // 
            // actual = target.WriteDelta(delta, file);
            // 
            // Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.XmlSerializationProvider<THash, TCheck>.WriteDelta did not " +
            //        "return the expected value.");
            // Assert.Inconclusive("Verify the correctness of this test method.");
        }
    }
}