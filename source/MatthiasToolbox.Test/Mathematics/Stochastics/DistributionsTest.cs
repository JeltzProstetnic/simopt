using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using MatthiasToolbox.Utilities;
using Moq;
using NUnit.Framework;

namespace MatthiasToolbox.Test.Mathematics.Stochastics
{
	[TestFixture]
	public class DistributionsTest
	{
        private List<Type> doubleDistributionTypes;
        private List<Type> integerDistributionTypes;
		private List<IDistribution<double>> doubleDistributions;
		private List<IDistribution<int>> integerDistributions;

		[SetUp]
		public void Init()
		{
            doubleDistributions = new List<IDistribution<double>>();
            integerDistributions = new List<IDistribution<int>>();
            doubleDistributionTypes = new List<Type>();
            integerDistributionTypes = new List<Type>();

            foreach (Type t in (typeof(IDistribution<double>)).GetDerivedTypes())
            {
                if (t.IsRealClass() && !t.FullName.Contains("Castle.Proxies"))
                {
                    doubleDistributionTypes.Add(t);
                    object oDistribution = Activator.CreateInstance(t);
                    doubleDistributions.Add((IDistribution<double>)oDistribution);
                }
            }

            foreach (Type t in (typeof(IDistribution<int>)).GetDerivedTypes())
            {
                if (t.IsRealClass() && !t.FullName.Contains("Castle.Proxies"))
                {
                    integerDistributionTypes.Add(t);
                    object oDistribution = Activator.CreateInstance(t);
                    integerDistributions.Add((IDistribution<int>)oDistribution);
                }
            }
		}

		[Test]
		public void TestConstructors()
		{
			foreach(Type t in doubleDistributionTypes)
			{
				object oDistribution = Activator.CreateInstance(t);
				IDistribution<double> iDistribution = (IDistribution<double>)oDistribution;
                Assert.IsFalse(iDistribution.Initialized);
                Assert.IsFalse(iDistribution.Configured);
				iDistribution.Initialize(123, false);
				Assert.IsTrue(iDistribution.Initialized);
				Assert.IsFalse(iDistribution.Configured);
					
				if(t.FullName.Contains("ConstantDoubleDistribution")
				  || t.FullName.Contains("ConstantIntegerDistribution")) continue;
					
				oDistribution = Activator.CreateInstance(t, 123, false);
				iDistribution = (IDistribution<double>)oDistribution;
				Assert.IsTrue(iDistribution.Initialized);
				Assert.IsFalse(iDistribution.Configured);
				Assert.IsFalse(iDistribution.Antithetic);
				Assert.AreEqual(123, iDistribution.Seed);
			}
			
			foreach(Type t in integerDistributionTypes)
			{
				object oDistribution = Activator.CreateInstance(t);
				IDistribution<int> iDistribution = (IDistribution<int>)oDistribution;
                Assert.IsFalse(iDistribution.Initialized);
                Assert.IsFalse(iDistribution.Configured);
				iDistribution.Initialize(123, false);
				Assert.IsTrue(iDistribution.Initialized);
				Assert.IsFalse(iDistribution.Configured);
					
				if(t.FullName.Contains("ConstantIntegerDistribution")) continue;
					
				oDistribution = Activator.CreateInstance(t, 123, false);
				iDistribution = (IDistribution<int>)oDistribution;
				Assert.IsTrue(iDistribution.Initialized);
				Assert.IsFalse(iDistribution.Configured);
				Assert.IsFalse(iDistribution.Antithetic);
				Assert.AreEqual(123, iDistribution.Seed);
			}
		}
		
		[Test]
		public void TestTriangular()
		{
			TriangularDistribution td = new TriangularDistribution();
			td.ConfigureMean(1100, 2040, 3600);
			td.Initialize(123, false);
			for(int i = 0; i<= 1000; i++)
			{
				double r = td.Next();
				Assert.GreaterOrEqual(r, 1100d);
				Assert.LessOrEqual(r, 3600d);
			}
		}
	}
}