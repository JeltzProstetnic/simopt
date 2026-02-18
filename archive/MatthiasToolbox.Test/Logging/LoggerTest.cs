using System;
using System.Collections.Generic;
using System.Diagnostics;
using MatthiasToolbox.Logging;
using Moq;
using NUnit.Framework;

namespace MatthiasToolbox.Test.Logging
{
	[TestFixture]
	public class LoggerTest : ILogger
	{
//		Mock<ILogger> mockLogger = new Mock<ILogger>();
		
		int loggingTestIndex = 0;
		
		[SetUp]
		public void Init()
		{
//			mockLogger.Setup(l => l.LoggingEnabled).Returns(true);
//			mockLogger.Setup(l => l.Log(It.IsAny<DateTime>(), It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Callback((timeStamp, sender, messageClass, message, data) => LoggerCallback(timeStamp, sender, messageClass, message, data));
//			Logger.Add(mockLogger.Object, new DefaultLoggerSettings());
		}
		
		[Test]
		public void TestLog()
		{
//			mockLogger.Setup(l => l.Log(It.IsAny<DateTime>(), It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Callback((timeStamp, sender, messageClass, message, data) => Assert.AreEqual("UndefinedSender", data["Sender"]));
//			ILogger logger = mockLogger.Object;
			Logger.Add(this, new DefaultLoggerSettings());
			Logger.Log("Hallo");
		}
		
		public bool LoggingEnabled {
			get {
				return true;
			}
			set {
				;
			}
		}
		
		public void Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
		{
			switch (loggingTestIndex) {
				case 0:
					Assert.AreEqual("UndefinedSender", data[Logger.Sender]);
					break;
				default:
					
					break;
			}
		}
		
		public void ShutdownLogger()
		{
			;
		}
	}
}
