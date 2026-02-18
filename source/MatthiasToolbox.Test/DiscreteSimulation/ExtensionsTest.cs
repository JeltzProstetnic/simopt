using System;
using System.Collections.Generic;
using MatthiasToolbox.Simulation;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Mathematics;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using Moq;
using NUnit.Framework;

namespace MatthiasToolbox.Test.DiscreteSimulation
{
	[TestFixture]
	public class ExtensionsTest
	{
		#region cvar
		
		private Mock<ISeedSource> mockSeedSource = new Mock<ISeedSource>();
		private Mock<IDistribution<double>> mockDistribution = new Mock<IDistribution<double>>();
		
		#endregion
		#region init
		
		[SetUp]
		public void Init()
		{
			mockDistribution.Setup(d => d.Configured).Returns(false);
			mockDistribution.Setup(d => d.Initialized).Returns(false);
			mockSeedSource.Setup(s => s.SeedGenerator).Returns(new UniformIntegerDistribution(0, 0, int.MaxValue, false));
		}
		
		#endregion
		#region impl

		[Test]
		public void TestRandomOfTExtensions()
		{
			ISeedSource seedSource = mockSeedSource.Object;
			IDistribution<double> distribution = mockDistribution.Object;
			
			try 
			{
				seedSource.Random<double>(null, false, false);
				Assert.Fail("ArgumentNullException expected!");
			} 
			catch { /* expected exception */ }
			
			try 
			{
				seedSource.Random<double>(distribution, false, false);
				Assert.Fail("ArgumentException expected!");
			} 
			catch { /* expected exception */ }
			
			mockDistribution.Setup(d => d.Configured).Returns(true);
			mockDistribution.Setup(d => d.Initialized).Returns(true);
			
			try 
			{
				seedSource.Random<double>(distribution, false, false);
				Assert.Fail("ArgumentException expected!");
			} 
			catch { /* expected exception */ }
			
			mockDistribution.Setup(d => d.Configured).Returns(true);
			mockDistribution.Setup(d => d.Initialized).Returns(false);
			Random<double> rnd = seedSource.Random<double>(distribution, false, false);
			
			mockSeedSource.Verify(s => s.AddRandomGenerator(rnd), Times.Once());
			mockDistribution.Verify(d => d.Initialize(1559595546, false), Times.Once());
			
			Assert.NotNull(rnd);
			Assert.IsFalse(rnd.Antithetic);
			Assert.IsFalse(rnd.NonStochasticMode);
		}
		
		[Test]
		public void TestTimeConversions() 
		{
			DateTime dateTime1 = new DateTime(2010, 6, 13, 15, 30, 59, 999);
			TimeSpan timeSpan1 = new TimeSpan(7, 23, 59, 59, 999);
			double dDateTime1 = dateTime1.ToDouble();
			double dTimeSpan1 = timeSpan1.ToDouble();
			DateTime dateTime2 = dDateTime1.ToDateTime();
			TimeSpan timeSpan2 = dTimeSpan1.ToTimeSpan();
			
			Assert.AreEqual(dateTime2.ToString("yyyy MM d HH mm ss fffff"), dateTime1.ToString("yyyy MM d HH mm ss fffff"));
			Assert.AreEqual(timeSpan2, timeSpan1);
			
			Assert.AreEqual(dateTime1, Extensions.Max(dateTime1, new DateTime(2009,1,2,3,4,5,6)));
			Assert.AreEqual(dateTime1, Extensions.Min(dateTime1, new DateTime(2011,1,2,3,4,5,6)));
			Assert.AreEqual(timeSpan1, Extensions.Max(timeSpan1, new TimeSpan(6,1,2,3,4)));
			Assert.AreEqual(timeSpan1, Extensions.Min(timeSpan1, new TimeSpan(8,1,2,3,4)));
			
			Assert.AreEqual(dDateTime1, dateTime1.TicksToDouble());
			Assert.AreEqual(dTimeSpan1, timeSpan1.TicksToDouble());
			
			Assert.AreEqual(dateTime1.ToString(), dDateTime1.ToDateTime().ToString());
			Assert.AreEqual(timeSpan1, dTimeSpan1.ToTimeSpan());
		}
		
		#endregion
	}
}