using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Mathematics.Geometry.Projections.Geography;
using MatthiasToolbox.Mathematics.Geometry;
using System.Diagnostics;
using MatthiasToolbox.Basics.Datastructures.Geometry;

namespace MatthiasToolbox.Mathematics.Test
{
    public static class TestGeometry
    {
        private static Random rnd;

        static TestGeometry()
        {
            rnd = new Random(123);
        }

        public static void RunAllTests()
        {
            TestRobinson();
        }

        public static void TestRobinson()
        {
            Robinson r = new Robinson();
            r.Initialize();

            // 51°30′29″N 0°7′29″W
            // 51.51667, -0.1 or 51.4791, 0
            //Point londonXY1 = r.Project(51.51667, -0.1);
            //Console.WriteLine(londonXY1);
            Point londonXY2 = r.Transform(new Point(51.51667, -0.1)); // this should be about 5730 km (3569 mi) by 6.93 km (4.3 mi)
            Console.WriteLine("London longitude & latitude = " + (new Point(londonXY2.X / 1000, londonXY2.Y / 1000)).ToString());

            //Point londonLL1 = r.ProjectInverse(5730000 / 6000000, 6930 / 6000000);
            //Console.WriteLine(londonLL1);
            Point londonLL2 = r.TransformInverse(new Point(5730000, 6930));
            Console.WriteLine("London distance from equator and prime meridian = " + londonLL2.ToString());

            //Point london1 = r.Project(5730000, 6930);
            //Console.WriteLine(london1);
            //Point london2 = r.Transform(new Point(5730000, 6930));
            //Console.WriteLine(london2);

            //Point london3 = r.ProjectInverse(51.51667, -0.1);
            //Console.WriteLine(london3);
            //Point london4 = r.TransformInverse(new Point(51.51667, -0.1));
            //Console.WriteLine(london4);

            // Sydney
            // 33° 51′ 35.9″ S, 151° 12′ 40″ E
            // -33.859972, 151.211111
            Point sydneyXY2 = r.Transform(new Point(-33.859972, 151.211111)); // this should be about 3769 km by 16832 km
            Console.WriteLine("Sydney longitude & latitude = " + (new Point(sydneyXY2.X / 1000, sydneyXY2.Y / 1000)).ToString());

            //Point sydneyLL2 = r.TransformInverse(new Point(3769000, 16832000)); // y out of bounds!
            //Console.WriteLine("Sydney distance from equator and prime meridian = " + sydneyLL2);



            Debug.Assert(r.ProjectInverse(0, 0).Equals(new Point(0, 0)));
            Debug.Assert(r.Project(0, 0) == new Point(0, 0));

            Debug.Assert(r.TransformInverse(new Point(0, 0)) == new Point(0, 0));
            Debug.Assert(r.Transform(new Point(0, 0)).Equals(new Point(0, 0)));
        }

    }
}
