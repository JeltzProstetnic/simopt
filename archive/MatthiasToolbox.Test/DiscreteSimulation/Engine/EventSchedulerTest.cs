/*
 * User: Matthias Gruber
 * Date: 2010-06-15
 * Time: 11:00
 * 
 * (C) Matthias Gruber
 */

using System;
using MatthiasToolbox.Simulation.Engine;
using Moq;
using NUnit.Framework;

namespace MatthiasToolbox.Test.DiscreteSimulation.Engine
{
	[TestFixture]
	public class EventSchedulerTest
	{
		#region cvar

		private Mock<IModel> mockModel = new Mock<IModel>();
		
		#endregion
		#region init
		
		[SetUp]
		public void Init()
		{
			// mockModel.Setup()
		}
		
		#endregion
		#region impl

		[Test]
		public void TestCtor()
		{
			IModel model = mockModel.Object;
			EventScheduler eventScheduler = new EventScheduler(model);
			Assert.AreEqual(0, eventScheduler.EventfulMomentsCount);
			// Assert.NotNull(eventScheduler.EventList);
			// Assert.NotNull(eventScheduler.Imm
		}
		
		#endregion
	}
}
