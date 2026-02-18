using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MatthiasToolbox.Mathematics.Stochastics;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;

namespace MatthiasToolbox.Mathematics.Test
{
    public static class TestStochastics
    {
        private static Random rnd;

        static TestStochastics()
        {
            rnd = new Random(123);
        }

        public static void RunAllTests()
        {
            TestGaussian();
        }

        public static void TestGaussian() 
        {
            Console.WriteLine("MatthiasToolbox.Mathematics.Stochastics.GaussianDistribution ...");

            Random<double> r = new Random<double>(new GaussianDistribution(rnd.Next(), 0, 1));


            Console.WriteLine("MatthiasToolbox.Mathematics.Stochastics.GaussianDistribution OK.");
        }

        public static IEnumerable<double> TestGaussianWithFeedback(int iterations = 1000)
        {
            Console.WriteLine("MatthiasToolbox.Mathematics.Stochastics.GaussianDistribution ...");

            Random<double> r = new Random<double>(new GaussianDistribution(rnd.Next(), 0, 15));

            for (int i = 0; i < iterations; i += 1)
            {
                yield return r.Next();
            }

            Console.WriteLine("MatthiasToolbox.Mathematics.Stochastics.GaussianDistribution OK.");
        }

        public static IEnumerable<double> TestTriangularWithFeedback(int iterations = 1000)
        {
            Console.WriteLine("MatthiasToolbox.Mathematics.Stochastics.TriangularDistribution ...");

            Random<double> r = new Random<double>(new TriangularDistribution(rnd.Next(), -40, 0, 40));

            for (int i = 0; i < iterations; i += 1)
            {
                yield return r.Next();
            }

            Console.WriteLine("MatthiasToolbox.Mathematics.Stochastics.TriangularDistribution OK.");
        }

        public static IEnumerable<double> TestErlangWithFeedback(int iterations = 1000)
        {
            Console.WriteLine("MatthiasToolbox.Mathematics.Stochastics.ErlangDistribution ...");

            Random<double> r = new Random<double>(new ErlangDistribution(rnd.Next(), 15, 3, -40));

            for (int i = 0; i < iterations; i += 1)
            {
                yield return r.Next();
            }

            Console.WriteLine("MatthiasToolbox.Mathematics.Stochastics.ErlangDistribution OK.");
        }
    }
}
