using System;
using NUnit.Framework;
using MatthiasToolbox.Basics.Utilities;
using System.Collections.Generic;

namespace MatthiasToolbox.Test.Basics.Utilities
{
	[TestFixture]
	public class MultiEnumeratorTest
	{
		[SetUp]
		public void Init()
		{
			// TODO: Add Init code.
		}

        [Test]
        public void TestMethod()
        {
            List<bool> a = new List<bool> { true, true, false, true, false, true, true, true };
            List<bool> b = new List<bool> { false, false, true, false, true, false };

            int i = 0;
            foreach (List<bool> item in new MultiEnumerable<bool>(a, b))
            {
                i++;
                Assert.IsTrue(item[0] ^ item[1]);
            }
            Assert.AreEqual(6, i);

            i = 0;
            foreach (List<bool> item in new MultiEnumerable<bool>(true, a, b))
            {
                i++;
                Assert.IsTrue(item[0] ^ item[1]);
            }
            Assert.AreEqual(8, i);
        }
	}
}
