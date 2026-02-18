using System;
using System.Collections.Generic;
using MatthiasToolbox.Basics.Datastructures.Collections;
using NUnit.Framework;

namespace MatthiasToolbox.Test.Basics.Collections
{
	[TestFixture]
	public class ReadOnlyDictionaryTest
	{
		private Dictionary<int, string> dummyDictionary;
		
		[SetUp]
		public void Init()
		{
			dummyDictionary = new Dictionary<int, string>();
			dummyDictionary[0] = "Hallo";
			dummyDictionary[3] = "du";
			dummyDictionary[6] = "Fauli";
			dummyDictionary[9] = "!";
		}
		
		[Test]
		public void TestReadOnlyDictionary()
		{
			// test ctor
			ReadOnlyDictionary<int, string> testDictionary = new ReadOnlyDictionary<int, string>(dummyDictionary);
			Assert.NotNull(testDictionary._dict);
			Assert.AreEqual(dummyDictionary, testDictionary._dict);
			
			// test prop
			Assert.AreEqual(dummyDictionary.Count, testDictionary.Count);
			Assert.IsTrue(testDictionary.IsReadOnly);
			Assert.AreEqual(dummyDictionary.Keys, testDictionary.Keys);
			Assert.AreEqual(dummyDictionary.Values, testDictionary.Values);
			
			foreach(int key in dummyDictionary.Keys)
				Assert.AreEqual(dummyDictionary[key], testDictionary[key]);
			
			try {
				testDictionary[5] = "This should be read only!";
				Assert.Fail();
			} catch { /* expected exception */ }
			
			// TODO: test impl
		}
	}
}
