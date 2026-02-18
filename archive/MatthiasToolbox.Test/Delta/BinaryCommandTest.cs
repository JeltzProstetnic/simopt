// complete

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using MatthiasToolbox.Delta.Utilities;

namespace MatthiasToolbox.Test.Delta
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.BinaryCommand and is intended
    ///to contain all BlueLogic.SDelta.Core.BinaryCommand Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class BinaryCommandTest
    {
    	// NUnit
    	[SetUp]
    	public void Initialize() {}
    	
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
        ///A test for BinaryCommand (CommandType, byte[], long, long)
        ///</summary>
        [Test] // [TestMethod()]
        public void ConstructorTest()
        {
            CommandType command = CommandType.Insert;
            byte[] data = { 0x01, 0x02, 0x02, 0x02 };

            BinaryCommand target = new BinaryCommand(command, data, long.MinValue, long.MaxValue, 0);

            Assert.AreEqual(data, target.Data);
            Assert.AreEqual(command, target.CommandType);
            Assert.AreEqual(long.MinValue, target.StartAddress);
            Assert.AreEqual(long.MaxValue, target.EndAddress);
        }

        /// <summary>
        ///A test for CommandType
        ///</summary>
        [Test] // [TestMethod()]
        public void CommandTypeTest()
        {
            CommandType command = CommandType.Copy;
            byte[] data = null;
            long startAddress = 0;
            long endAddress = 0;

            BinaryCommand target = new BinaryCommand(command, data, startAddress, endAddress, 0);
            target.CommandType = CommandType.Insert;

            Assert.AreEqual(CommandType.Insert, target.CommandType, "BlueLogic.SDelta.Core.BinaryCommand.CommandType was not set correctly.");
        }

        /// <summary>
        ///A test for Data
        ///</summary>
        [Test] // [TestMethod()]
        public void DataTest()
        {
            CommandType command = CommandType.Noop;
            byte[] data = null;
            long startAddress = 0;
            long endAddress = 0;

            BinaryCommand target = new BinaryCommand(command, data, startAddress, endAddress, 0);

            byte[] val = { 0x01, 0x02, 0x02, 0x02 };
            target.Data = val;

            CollectionAssert.AreEqual(val, target.Data, "BlueLogic.SDelta.Core.BinaryCommand.Data was not set correctly.");
        }

        /// <summary>
        ///A test for EndAddress
        ///</summary>
        [Test] // [TestMethod()]
        public void EndAddressTest()
        {
            CommandType command = CommandType.Noop;
            byte[] data = null;
            long startAddress = 0;
            long endAddress = 0;

            BinaryCommand target = new BinaryCommand(command, data, startAddress, endAddress, 0);

            long val = long.MaxValue;
            target.EndAddress = val;


            Assert.AreEqual(val, target.EndAddress, "BlueLogic.SDelta.Core.BinaryCommand.EndAddress was not set correctly.");
        }

        /// <summary>
        ///A test for StartAddress
        ///</summary>
        [Test] // [TestMethod()]
        public void StartAddressTest()
        {
            CommandType command = CommandType.Noop;
            byte[] data = null;
            long startAddress = 0;
            long endAddress = 0;

            BinaryCommand target = new BinaryCommand(command, data, startAddress, endAddress, 0);

            long val = long.MaxValue;
            target.StartAddress = val;

            Assert.AreEqual(val, target.StartAddress, "BlueLogic.SDelta.Core.BinaryCommand.StartAddress was not set correctly.");
        }

        /// <summary>
        ///A test for BinaryCommand (CommandType, byte[], long, long, int)
        ///</summary>
        [Test] // [TestMethod()]
        public void ConstructorTest1()
        {
            CommandType command = CommandType.Noop;
            byte[] data = {13, 12};
            long startAddress = 0;
            long endAddress = 0;
            int repeat = 0;

            BinaryCommand target = new BinaryCommand(command, data, startAddress, endAddress, repeat);

            Assert.AreEqual(data, target.Data);
        }

        /// <summary>
        ///A test for Equals (object)
        ///</summary>
        [Test] // [TestMethod()]
        public void EqualsTest()
        {
            byte[] bb = { 12, 13 };
            BinaryCommand a = new BinaryCommand(CommandType.Insert, bb, 0, 0, 0);
            BinaryCommand b = new BinaryCommand(CommandType.Insert, bb, 0, 0, 0);
            
            bool expected = true;
            bool actual;

            actual = a.Equals(b);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.BinaryDelta.BinaryCommand.Equals did not return the expecte" +
                    "d value.");
        }

        
        /// <summary>
        ///A test for operator != (BinaryCommand, BinaryCommand)
        ///</summary>
        [Test] // [TestMethod()]
        public void InequalityTest()
        {
            byte[] aa = { 13, 13 };
            byte[] bb = { 12, 13 };
            BinaryCommand a = new BinaryCommand(CommandType.Insert, aa, 0, 0, 0);
            BinaryCommand b = new BinaryCommand(CommandType.Insert, bb, 1, 2, 3);

            bool expected = true;
            bool actual;

            actual = a != b;

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.BinaryDelta.BinaryCommand.operator != did not return the ex" +
                    "pected value.");
        }

        /// <summary>
        ///A test for operator == (BinaryCommand, BinaryCommand)
        ///</summary>
        [Test] // [TestMethod()]
        public void EqualityTest()
        {
            byte[] bb = {12, 13};
            BinaryCommand a = new BinaryCommand(CommandType.Insert, bb, 0, 0, 0);
            BinaryCommand b = new BinaryCommand(CommandType.Insert, bb, 0, 0, 0);

            bool expected = true;
            bool actual;

            actual = a == b;

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.BinaryDelta.BinaryCommand.operator == did not return the ex" +
                    "pected value.");
        }

        /// <summary>
        ///A test for Repeat
        ///</summary>
        [Test] // [TestMethod()]
        public void RepeatTest()
        {
            CommandType command = CommandType.Copy;
            byte[] data = null;
            long startAddress = 1;
            long endAddress = 10;
            int repeat = 3;

            BinaryCommand target = new BinaryCommand(command, data, startAddress, endAddress, repeat);

            int val = 4;
            target.Repeat = val;

            Assert.AreEqual(val, target.Repeat, "BlueLogic.SDelta.Core.BinaryDelta.BinaryCommand.Repeat was not set correctly.");
        }
    } // class
} // namespace
