using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using MatthiasToolbox.Semantics.Interfaces;

namespace MatthiasToolbox.Test.Semantics.Engine
{
    [TestFixture]
    public class TaxonomyTest
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
        public void TestSomething()
        {
   
        }

        [Test]
        public void TestConstructors()
        {
//            //Test string constructor
//            string name = "Test Taxonomy 1";
//            Taxonomy t1 = new Taxonomy(name);
//            Assert.AreEqual(t1.Name, name);
////            Assert.IsNotNull(t1.Edges);

//            //the following 3 are accessors to the same field
//            Assert.IsNotNull(t1.Vertices);
//            Assert.IsNotNull(t1.Content); 
//            Assert.IsNotNull(t1.Nodes);

//            Assert.IsNull(t1.Root);
        }

        #endregion
    }
}