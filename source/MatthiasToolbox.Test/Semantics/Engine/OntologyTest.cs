using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using MatthiasToolbox.Semantics.Interfaces;
using MatthiasToolbox.Semantics.Utilities;

namespace MatthiasToolbox.Test.Semantics.Engine
{
    [TestFixture]
    public class OntologyTest
    {
        #region cvar

        private Mock<IConcept> mockConcept = new Mock<IConcept>();

        #endregion
        #region init

        [SetUp]
        public void Init()
        {
            //mockDistribution.Setup(d => d.Configured).Returns(false);
            //mockDistribution.Setup(d => d.Initialized).Returns(false);
            //mockConcept.Setup(s => s.SeedGenerator).Returns(new UniformIntegerDistribution(0, 0, int.MaxValue, false));
        }

        #endregion
        #region impl

        [Test]
        public void TestSynSet()
        {
            //SynSet set1 = new SynSet();

            //// must be empty, but must not throw exception
            //foreach (ITerm t in set1.Members) Assert.Fail();
            //foreach (String s in set1.MemberNames) Assert.Fail();

            //// TODO  Semantics - replace this with moq objects - we cannot assume Term to work correctly.
            //Term t1 = new Term("Test Term 1");
            //Term t2 = new Term("Test Term 2");
            //Term t3 = "Test Term 3";// new Term("Test Term 3");

            //Assert.IsTrue(set1.Add(t1));
            //Assert.IsFalse(set1.Add(t1));
            //// Assert.ReferenceEquals(set1, t1.SynSet);

            //// TODO  Semantics - complete tests.
        }

        [Test]
        public void TestTerm()
        {
            //Term t = new Term("Test Term");

            //// TODO  Semantics - complete tests.
        }

        #endregion
    }
}