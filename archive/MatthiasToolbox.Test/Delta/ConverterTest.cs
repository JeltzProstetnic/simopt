// complete (except ConvertCollection overload and ConvertObject functions)

using NUnit.Framework; // using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections;

namespace MatthiasToolbox.Test.Delta
{
    /// <summary>
    ///This is a test class for BlueLogic.SDelta.Core.Converter and is intended
    ///to contain all BlueLogic.SDelta.Core.Converter Unit Tests
    ///</summary>
    [TestFixture] // [TestClass()]
    public class ConverterTest
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
        ///A test for Convert&lt;,&gt; (S)
        ///</summary>
        [Test] // [TestMethod()]
        public void ConvertTest()
        {
            double source = 0.123; // TODO: Initialize to an appropriate value

            String expected = "0#123";
            String actual;

            actual = MatthiasToolbox.Delta.Utilities.Converter.Convert<double, String>(source).Replace('.', '#').Replace(',', '#');

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.Converter.Convert<S, T> did not return the expected value.");
        }

        /// <summary>
        ///A test for Convert&lt;,&gt; (S, T)
        ///</summary>
        [Test] // [TestMethod()]
        public void ConvertTest1()
        {
            double errorValue = -1;
            double expected = errorValue;
            double actual;

            actual = MatthiasToolbox.Delta.Utilities.Converter.Convert<String, double>(null, errorValue);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.Converter.Convert<S, T> did not return the expected value.");
        }

        /// <summary>
        ///A test for Convert&lt;,&gt; (S, T, IFormatProvider, ref bool)
        ///</summary>
        [Test] // [TestMethod()]
        public void ConvertTest2()
        {
            int source = 127;
            String errorValue = "error";
            IFormatProvider formatter = null;

            bool success = false;
            bool success_expected = true;

            String expected = "127";
            String actual;

            actual = MatthiasToolbox.Delta.Utilities.Converter.Convert<int, String>(source, errorValue, formatter, ref success);

            Assert.AreEqual(success_expected, success, "success_Convert_expected was not set correctly.");
            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.Converter.Convert<S, T> did not return the expected value.");
        }

        /// <summary>
        ///A test for ConvertByInterface (IConvertible, Type, IFormatProvider)
        ///</summary>
        // [DeploymentItem("BlueLogic.SDelta.Core.dll")]
        [Test] // [TestMethod()]
        public void ConvertByInterfaceTest()
        {
            IConvertible source = "123";
            Type targetType = typeof(int);
            IFormatProvider formatter = null;

            int expected = 123;
            int actual;

            actual = (int)BlueLogic_SDelta_Core_ConverterAccessor.ConvertByInterface(source, targetType, formatter);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.Converter.ConvertByInterface did not return the expected va" +
                    "lue.");
        }

        /// <summary>
        ///A test for ConvertCollection (IEnumerable, IList, object)
        ///</summary>
        [Test] // [TestMethod()]
        public void ConvertCollectionTest()
        {
            List<String> source = new List<String>();
            ArrayList target = new ArrayList();
            ArrayList result = new ArrayList();
            source.Add("1");
            source.Add("2");
            source.Add("3");
            result.Add(null);
            result.Add("1");
            result.Add("2");
            result.Add("3");
            String errorValue = "error";
            bool expected = true;
            bool actual;

            actual = MatthiasToolbox.Delta.Utilities.Converter.ConvertCollection(source, target, errorValue);

            Assert.AreEqual(expected, actual, "BlueLogic.SDelta.Core.Converter.ConvertCollection did not return the expected value.");
            int i = 0;
            foreach (String val in result)
            {
                Assert.AreEqual(val, target[i]);
                i += 1;
            }
        }

    } // class
} // namespace
