using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Mathematics.Numerics;
using System.Diagnostics;
using MatthiasToolbox.Mathematics.Numerics.Matrices;
using Matrix = MatthiasToolbox.Mathematics.Numerics.Matrices.Matrix;

namespace MatthiasToolbox.Mathematics.Test
{
    // TODO: convert to NUnit tests!
    public static class TestNumerics
    {
        private static Random rnd;

        static TestNumerics()
        {
            rnd = new Random(123);
        }

        public static void RunAllTests()
        {
            TestComplex();
            TestMatrix();
        }

        public static void TestComplex() 
        {
            Console.WriteLine("MatthiasToolbox.Mathematics.Numerics.Complex ...");

            // create some test numbers
            Complex c1 = new Complex();
            Complex c2 = new Complex(1, 1);
            Complex c3 = new Complex(0, 0);
            Complex c4 = new Complex(2, 3);
            Complex i = new Complex(0, 1);
            Complex cOne = new Complex(1, 0);

            // testing ToString()
            Debug.Assert(c1.ToStringElaborate() == "0");
            Debug.Assert(cOne.ToStringElaborate() == "1");
            Debug.Assert(i.ToStringElaborate() == "1i");
            Debug.Assert(c2.ToStringElaborate() == "(1,1)");
            
            // writing something to the console 
            // so that it doesn't get too boring
            Console.WriteLine(i.ToString());
            Console.WriteLine(cOne.ToString());
            Console.WriteLine(c1.ToString());
            Console.WriteLine(c2.ToString());

            // testing ctors and properties
            Debug.Assert(c1.IsReal);
            Debug.Assert(!c1.IsImaginary);
            Debug.Assert(c1.Real == 0);
            Debug.Assert(c1.Imaginary == 0);
            
            Debug.Assert(!c2.IsReal);
            Debug.Assert(!c2.IsImaginary);
            Debug.Assert(c2.Real == 1);
            Debug.Assert(c2.Imaginary == 1);

            Debug.Assert(cOne.IsReal);
            Debug.Assert(!cOne.IsImaginary);
            Debug.Assert(cOne.Real == 1);
            Debug.Assert(cOne.Imaginary == 0);

            Debug.Assert(!i.IsReal);
            Debug.Assert(i.IsImaginary);
            Debug.Assert(i.Real == 0);
            Debug.Assert(i.Imaginary == 1);

            // testing comparisons, they
            // are prerequisites for further tests
            Debug.Assert(c1 == c3);
            Debug.Assert(c1.Equals(c3));
            Debug.Assert(c1 <= c3);
            Debug.Assert(c1 >= c3);
            Debug.Assert(!(c1 != c3));
            Debug.Assert(!(c1 > c3));
            Debug.Assert(!(c1 < c3));

            Debug.Assert(!(c1 == cOne));
            Debug.Assert(!(c1 == c2));
            Debug.Assert(!(c1 == i));

            Debug.Assert(!c1.Equals(cOne));
            Debug.Assert(!c1.Equals(c2));
            Debug.Assert(!c1.Equals(i));

            Debug.Assert(c1 != cOne);
            Debug.Assert(c1 != c2);
            Debug.Assert(c1 != i);

            Debug.Assert(c1 <= cOne);
            Debug.Assert(!(c1 >= cOne));
            Debug.Assert(!(c1 > cOne));
            Debug.Assert(c1 < cOne);

            // cloning
            Debug.Assert(c4.Equals(c4.Clone()));

            // testing constants 
            Debug.Assert(Complex.Zero == c1);
            Debug.Assert(Complex.One == cOne);
            Debug.Assert(Complex.i == i);

            // testing operator overloads
            // cast
            Debug.Assert(cOne == 1d);
            Debug.Assert(c1 == 0d);
            Debug.Assert(cOne != 0d);
            Debug.Assert(c1 != 1d);
            Debug.Assert(new double[] { 0, 0 } == c1);
            Debug.Assert(new double[] { 1, 0 } == cOne);
            Debug.Assert(new double[] { 0, 1 } == i);
            Debug.Assert(new double[] { 1, 1 } == c2);

            // +
            Debug.Assert(c1 + cOne == cOne);
            Debug.Assert(cOne + cOne == 2);
            Debug.Assert(cOne + i == c2);
            Debug.Assert(c2 + c2 == new double[] { 2, 2 });

            // -
            Debug.Assert(cOne - cOne == 0);
            Debug.Assert(i - i == 0);

            // *

            // /

            // ^ is tested later because it depends on
            // Pow, Exp, Log and Arg

            // testing mathematical functions
            Complex c1inv = c1.Invert();
            Complex c1con = c1.Conjugate();
            Complex c2inv = c2.Invert();
            Complex c2con = c2.Conjugate();
            
            Debug.Assert(Complex.Abs(c1) == c1);

            Debug.Assert(double.IsNaN(Complex.Arg(0)));
            Debug.Assert(Complex.Arg(cOne) == 0);
            Debug.Assert(Complex.Arg(i) == Math.PI / 2);
            double d = Complex.Arg(new double[] { 0, -1 });
            Debug.Assert(Complex.Arg(new double[] { 0, -1 }) == -Math.PI/2);
            Debug.Assert(Complex.Arg(c2) == Math.PI / 4);
            Debug.Assert(Complex.Arg(-1) == Math.PI);

            // Debug.Assert(Complex.Cos
            
            // Debug.Assert(Complex.Cosh
            
            // Debug.Assert(Complex.Cot
            
            // Debug.Assert(Complex.Coth
            
            // Debug.Assert(Complex.Csch
            
            // Debug.Assert(Complex.Exp
            
            // Debug.Assert(Complex.Log
            
            // Debug.Assert(Complex.Pow
            
            // Debug.Assert(Complex.Sech
            
            // Debug.Assert(Complex.Sin
            
            // Debug.Assert(Complex.Sinh
            
            // Debug.Assert(Complex.Sqrt
            Debug.Assert(Complex.Sqrt(-1d) == i);
            
            // Debug.Assert(Complex.Tan
            
            // Debug.Assert(Complex.Tanh

            // ^
            Debug.Assert((cOne ^ 2) == 1);
            Debug.Assert((new Complex(2, 0) ^ 2) == 4);

            Console.WriteLine("MatthiasToolbox.Mathematics.Numerics.Complex OK.");
        }

        public static void TestMatrix()
        {
            Console.WriteLine("MatthiasToolbox.Mathematics.Numerics.Matrices.Matrix ...");
            
            // ctors
            Matrix m1 = new Matrix(2, 2);
            Matrix m2 = new Matrix(3, 3);
            Matrix m3 = new Matrix(4, 4);
            Matrix m4 = new Matrix(5, 5);
            Matrix m5 = new Matrix(20, 30);
            Matrix m6 = new Matrix(30, 20);
            // ...

            foreach (MatrixRow row in m6.Rows)
            {
                foreach (Complex c in row)
                {
                    Debug.Assert(c == 0);
                }
            }

            Matrix mRnd = new Matrix(5, 2);
            for (int i = 1; i <= mRnd.RowCount; i++)
            {
                for (int j = 1; j <= mRnd.ColumnCount; j++)
                {
                    double x = (rnd.NextDouble() - 0.5) * rnd.Next();
                    double y = (rnd.NextDouble() - 0.5) * rnd.Next();
                    mRnd[i, j] = new Complex(x, y);
                }
            }
            mRnd[2, 2] = Complex.DivideSafe(mRnd[2, 2], 0d);
            mRnd[4, 1] = double.NegativeInfinity;

            Console.WriteLine(mRnd.ToString());

            Matrix m1clone = new Matrix(2, 2);
            Debug.Assert(m1.Equals(m1clone));

            Console.WriteLine("MatthiasToolbox.Mathematics.Numerics.Matrices.Matrix OK.");
        }
    }
}
