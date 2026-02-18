using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Mathematics;
using MatthiasToolbox.Mathematics.Enum;
using MatthiasToolbox.Basics.Datastructures.Geometry;

namespace MatthiasToolbox.Mathematics.Geometry.Projections.Geography
{
    /// <summary>
    /// 
    /// </summary>
    public class Robinson : PseudoCylindrical
    {
        #region cvar

        private static int nodesCount = 18;
        private static double FXC = 0.8487;
        private static double FYC = 1.3523;
        private static double C1 = 11.45915590261646417544;
        private static double RC1 = 0.08726646259971647884;
        private static double ONEEPS = 1.000001;
        private static double EPS = 1e-8;

        #region robinson tables

        private static double[] X = {
		1,	    -5.67239e-12,	-7.15511e-05,	 3.11028e-06,
		0.9986,	-0.000482241,	-2.48970e-05,	-1.33094e-06,
		0.9954,	-0.000831031,	-4.48610e-05,	-9.86588e-07,
		0.9900,	-0.001353630,	-5.96598e-05,	 3.67749e-06,
		0.9822,	-0.001674420,	-4.49750e-06,	-5.72394e-06,
		0.9730,	-0.002148690,	-9.03565e-05,	 1.88767e-08,
		0.9600,	-0.003050840,	-9.00732e-05,	 1.64869e-06,
		0.9427,	-0.003827920,	-6.53428e-05,	-2.61493e-06,
		0.9216,	-0.004677470,	-0.000104566,	 4.81220e-06,
		0.8962,	-0.005362220,	-3.23834e-05,	-5.43445e-06,
		0.8679,	-0.006093640,	-0.000113900,	 3.32521e-06,
		0.8350,	-0.006983250,	-6.40219e-05,	 9.34582e-07,
		0.7986,	-0.007553370,	-5.00038e-05,	 9.35532e-07,
		0.7597,	-0.007983250,	-3.59716e-05,	-2.27604e-06,
		0.7186,	-0.008513660,	-7.01120e-05,	-8.63072e-06,
		0.6732,	-0.009862090,	-0.000199572,	 1.91978e-05,
		0.6213,	-0.010418000,	 8.83948e-05,	 6.24031e-06,
		0.5722,	-0.009066010,	 0.000181999,	 6.24033e-06,
		0.5322,  0,              0,              0           };

        private static double[] Y = {
		0,	    0.0124,	     3.72529e-10,	 1.15484e-09,
		0.062,	0.0124001,	 1.76951e-08,	-5.92321e-09,
		0.124,	0.0123998,	-7.09668e-08,	 2.25753e-08,
		0.186,	0.0124008,	 2.66917e-07,	-8.44523e-08,
		0.248,	0.0123971,	-9.99682e-07,	 3.15569e-07,
		0.310,	0.0124108,	 3.73349e-06,	-1.17790e-06,
		0.372,	0.0123598,	-1.39350e-05,	 4.39588e-06,
		0.434,	0.0125501,	 5.20034e-05,	-1.00051e-05,
		0.4968,	0.0123198,	-9.80735e-05,	 9.22397e-06,
		0.5571,	0.0120308,	 4.02857e-05,	-5.29010e-06,
		0.6176,	0.0120369,	-3.90662e-05,	 7.36117e-07,
		0.6769,	0.0117015,	-2.80246e-05,	-8.54283e-07,
		0.7346,	0.0113572,	-4.08389e-05,	-5.18524e-07,
		0.7903,	0.0109099,	-4.86169e-05,	-1.07180e-06,
		0.8435,	0.0103433,	-6.46934e-05,	 5.36384e-09,
		0.8936,	0.00969679,	-6.46129e-05,	-8.54894e-06,
		0.9394,	0.00840949,	-0.000192847,	-4.21023e-06,
		0.9761,	0.00616525,	-0.000256001,	-4.21021e-06,
		1,      0,           0,              0           };

        #endregion

        #endregion
        #region ctor

        public Robinson()
            : base()
        {
        }

        #endregion
        #region impl

        public override Point Project(double lambda, double phi)
        {
            Point result = new Point();

            double iphi = Math.Abs(phi);
            int i = (int)Math.Floor(iphi * C1);

            if (i >= nodesCount) i = nodesCount - 1;

            iphi = Units.Convert(iphi - RC1 * (double)i, AngleUnit.Radians, AngleUnit.Degrees);

            i *= 4;

            result.X = PolyX(i, iphi) * FXC * lambda;
            result.Y = PolyY(i, iphi) * FYC;

            if (phi < 0.0) result.Y = -result.Y;

            return result;
        }

        public override Point ProjectInverse(double x, double y)
        {
            Point result = new Point();
            int i;
            double t;
            double t1;

            result.X = x / FXC;
            result.Y = Math.Abs(y / FYC);

            if (result.Y >= 1d)
            {
                if (result.Y > ONEEPS)
                {
                    throw new ApplicationException("Problem at inverse calculation: calculated Y value more than 1 plus tolerance!");
                }
                else
                {
                    result.Y = y < 0 ? -MMath.HalfPi : MMath.HalfPi;
                    result.X /= X[4 * nodesCount];
                }
            }
            else
            {
                double Tc0;
                double Tc1;
                double Tc2;
                double Tc3;

                i = (int)(4d * Math.Floor(result.Y * (double)nodesCount));

                while (true)
                {
                    if (Y[i] > result.Y) i -= 4;
                    else if (Y[i + 4] <= result.Y) i += 4;
                    else break;
                }

                t = 5d * (result.Y - Y[i]) / (Y[i + 4] - Y[i]); // WTF???

                Tc0 = Y[i];
                Tc1 = Y[i + 1];
                Tc2 = Y[i + 2];
                Tc3 = Y[i + 3];

                t = 5d * (result.Y - Tc0) / (Y[i + 1] - Tc0); // same as above except for Y[i + 1] instead of 4
                Tc0 -= result.Y;

                while (true) // Newton-Raphson
                {
                    t1 = (Tc0 + t * (Tc1 + t * (Tc2 + t * Tc3))) / (Tc1 + t * (Tc2 + Tc2 + t * 3d * Tc3));
                    t -= t1;
                    if (Math.Abs(t1) < EPS) break;
                }

                result.Y = Units.Convert(5d * (double)i + t, AngleUnit.Degrees, AngleUnit.Radians);

                if (y < 0) result.Y = -result.Y;

                result.X /= PolyX(i, t);
            }

            return result;
        }

        #endregion
        #region util

        private static double PolyX(int offset, double z)
        {
            return (X[offset] + z * (X[offset + 1] + z * (X[offset + 2] + z * X[offset + 3])));
        }

        private static double PolyY(int offset, double z)
        {
            return (Y[offset] + z * (Y[offset + 1] + z * (Y[offset + 2] + z * Y[offset + 3])));
        }

        #endregion
    }
}
