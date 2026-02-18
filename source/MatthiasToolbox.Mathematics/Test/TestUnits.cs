using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MatthiasToolbox.Mathematics.Enum;

namespace MatthiasToolbox.Mathematics.Test
{
    public static class TestUnits
    {
        private static Random rnd;

        static TestUnits()
        {
            rnd = new Random(123);
        }

        public static void RunAllTests()
        {
            TestConversions();
        }

        public static void TestConversions()
        {
            Debug.Assert(Units.Convert(Math.PI, AngleUnit.Radians, AngleUnit.Degrees) == 180);
            Debug.Assert(Units.Convert(180, AngleUnit.Degrees, AngleUnit.Radians) == Math.PI);

            try
            {
                Units.Convert(3, AngleUnit.Degrees, TimeUnit.Seconds);
                Debug.Assert(false, "An exception should have occured when converting angle to time!");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("Expected exception: " + ex.Message);
                Debug.Assert(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected exception: " + ex.Message);
                Debug.Assert(false, "An ArgumentException should have occured when converting angle to time!");
            }
        }
    }
}
