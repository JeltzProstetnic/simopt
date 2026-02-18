// TODO: problem writing / reading / debugging MemoryStream

using System.Collections;
using System.IO;
using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml;

namespace MatthiasToolbox.Test.Delta
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.Hashtable&lt;TKey, TValue&gt; and is intended
    ///to contain all BlueLogic.SDelta.Core.Hashtable&lt;TKey, TValue&gt; Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class HashtableTest
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
        ///A test for ReadXml (XmlReader)
        ///</summary>
        //[TestMethod()]
        //public void ReadWriteXmlTest()
        //{
        //    Hashtable<int, int> target = new Hashtable<int, int>();
        //    Hashtable<int, int> empty = new Hashtable<int, int>();
            
        //    for(int i = 1; i<=20; i++)
        //    {
        //        target.Add(i, 100-i);
        //    }
        //    XmlWriterSettings set = new XmlWriterSettings();
        //    set.ConformanceLevel = ConformanceLevel.Fragment;

        //    XmlReaderSettings setr = new XmlReaderSettings();
        //    set.ConformanceLevel = ConformanceLevel.Fragment;
            
        //    Stream s = new MemoryStream();
        //    XmlWriter w = XmlWriter.Create(s, set);
        //    target.WriteXml(w);
        //    s.Flush();
        //    s.Seek(0,SeekOrigin.Begin);
        //    XmlReader reader = XmlReader.Create(s, setr);
        //    empty.ReadXml(reader);

        //    for(int i = 1; i<=20; i++)
        //    {
        //        Assert.AreEqual(target[i], empty[i]);
        //    }
        //}
    }
}