// complete

using System;
using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.IO;
using MatthiasToolbox.Cryptography.Utilities;

namespace MatthiasToolbox.Test.Utilities
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.Utilities.PostfixStream and is intended
    ///to contain all BlueLogic.SDelta.Core.Utilities.PostfixStream Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class PostfixStreamTest
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
        ///A test for CanRead
        ///</summary>
        [Test] // [TestMethod()]
        public void CanReadTest()
        {
            Stream original = new MemoryStream(100);
            byte[] postFix = Encoding.UTF8.GetBytes("Blabla");
            PostfixStream target = new PostfixStream(original, postFix);

            Assert.AreEqual(true, target.CanRead, "BlueLogic.SDelta.Core.Utilities.PostfixStream.CanRead was not set correctly.");
        }

        /// <summary>
        ///A test for CanSeek
        ///</summary>
        [Test] // [TestMethod()]
        public void CanSeekTest()
        {
            Stream original = new MemoryStream(100);
            byte[] postFix = Encoding.UTF8.GetBytes("Blabla");
            PostfixStream target = new PostfixStream(original, postFix);

            Assert.AreEqual(false, target.CanSeek, "BlueLogic.SDelta.Core.Utilities.PostfixStream.CanRead was not set correctly.");
        }

        /// <summary>
        ///A test for CanWrite
        ///</summary>
        [Test] // [TestMethod()]
        public void CanWriteTest()
        {
            byte[] stream = Encoding.UTF8.GetBytes("Blablabla");
            Stream original = new MemoryStream(stream, true);
            byte[] postFix = Encoding.UTF8.GetBytes("Blabla");
            PostfixStream target = new PostfixStream(original, postFix);

            Assert.AreEqual(false, target.CanWrite, "BlueLogic.SDelta.Core.Utilities.PostfixStream.CanRead was not set correctly.");
        }

        /// <summary>
        ///A test for Flush ()
        ///</summary>
        [Test] // [TestMethod()]
        public void FlushTest()
        {
            Stream original = new MemoryStream(100);
            byte[] postFix = null;
            PostfixStream target = new PostfixStream(original, postFix);
            target.Flush();
        }

        /// <summary>
        ///A test for Length
        ///</summary>
        [Test] // [TestMethod()]
        public void LengthTest()
        {
            byte[] stream = Encoding.UTF8.GetBytes("Blablabla");
            Stream original = new MemoryStream(stream);
            byte[] postFix = Encoding.UTF8.GetBytes("Blabla");
            PostfixStream target = new PostfixStream(original, postFix);

            long val = 15;
            Assert.AreEqual(val, target.Length, "BlueLogic.SDelta.Core.Utilities.PostfixStream.Length was not set correctly.");
        }

        /// <summary>
        ///A test for Position
        ///</summary>
        [Test] // [TestMethod()]
        public void PositionTest()
        {
            Stream original = new MemoryStream(100);
            byte[] postFix = null;
            PostfixStream target = new PostfixStream(original, postFix);

            long val = 12;
            try
            {
                target.Position = val;
                Assert.Fail("postfixstream should not be able to seek!");
            }
            catch
            {
                long l = 0;
                Assert.AreEqual(l, target.Position, "BlueLogic.SDelta.Core.Utilities.PostfixStream.Position was not set correctly.");
            }
        }

        /// <summary>
        ///A test for Read (byte[], int, int)
        ///</summary>
        [Test] // [TestMethod()]
        public void ReadTest()
        {
            byte[] stream = Encoding.UTF8.GetBytes("Blablabla");
            Stream original = new MemoryStream(stream);
            byte[] postFix = Encoding.UTF8.GetBytes("Blabla");
            PostfixStream target = new PostfixStream(original, postFix);

            byte[] buffer = new byte[20];
            int offset = 0;
            int count = 20;

            int expected = 15;
            int actual;

            actual = target.Read(buffer, offset, count);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.Utilities.PostfixStream.Read did not return the expected value.");
            String s = Encoding.UTF8.GetString(buffer).Substring(0, actual);
            Assert.AreEqual(s, "BlablablaBlabla");
        }

        /// <summary>
        ///A test for Seek (long, SeekOrigin)
        ///</summary>
        [Test] // [TestMethod()]
        public void SeekTest()
        {
            Stream original = new MemoryStream(100);
            byte[] postFix = null;
            PostfixStream target = new PostfixStream(original, postFix);

            long offset = 0;
            SeekOrigin origin = SeekOrigin.Begin;

            try
            {
                target.Seek(offset, origin);
                Assert.Fail("postfixstream should not be able to seek!");
            }
            catch
            {
                long l = 0;
                Assert.AreEqual(l, target.Position);
            }
        }

        /// <summary>
        ///A test for SetLength (long)
        ///</summary>
        [Test] // [TestMethod()]
        public void SetLengthTest()
        {
            Stream original = new MemoryStream(100);
            byte[] postFix = null;
            PostfixStream target = new PostfixStream(original, postFix);

            try
            {
                target.SetLength(10);
                Assert.Fail("a postfix stream should not be changeable in length");
            }
            catch(NotSupportedException ex)
            {
                long l = 0;
                Assert.AreEqual(l, target.Position);
                Assert.IsNotNull(ex);
            }
        }

        /// <summary>
        ///A test for Write (byte[], int, int)
        ///</summary>
        [Test] // [TestMethod()]
        public void WriteTest()
        {
            byte[] stream = Encoding.UTF8.GetBytes("Blablabla");
            Stream original = new MemoryStream(stream, true);
            byte[] postFix = Encoding.UTF8.GetBytes("Blabla");
            PostfixStream target = new PostfixStream(original, postFix);
            
            int offset = 0;
            int count = 6;

            try
            {
                target.Write(postFix, offset, count);
                Assert.Fail("a postfix stream should not be writable");
            }
            catch(NotSupportedException ex)
            {
                long p = 0;
                Assert.AreEqual(p, target.Position);
                Assert.IsNotNull(ex);
            }
        }

    } // class
} // namespace
