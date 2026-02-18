// Accord Statistics Library
// Accord.NET framework
// http://www.crsouza.com
//
// Copyright © César Souza, 2009
// cesarsouza at gmail.com
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Accord.Statistics.Models.Regression.Linear
{
    /// <summary>
    ///   Simple Linear Regression of the form y = Ax + B.
    /// </summary>
    /// <remarks>
    ///   In linear regression, the model specification is that the dependent
    ///   variable, y is a linear combination of the parameters (but need not
    ///   be linear in the independent variables). As the linear regression
    ///   has a closed form solution, the regression coefficients can be
    ///   efficiently computed using the Regress method of this class.
    public class SimpleLinearRegression : ILinearRegression
    {
        private MultipleLinearRegression regression;

        /// <summary>
        ///   Creates a new Simple Linear Regression of the form y = Ax + B.
        /// </summary>
        public SimpleLinearRegression()
        {
            this.regression = new MultipleLinearRegression(2);
        }

        /// <summary>
        ///   Angular coefficient (Slope).
        /// </summary>
        public double Slope
        {
            get { return regression.Coefficients[1]; }
        }

        /// <summary>
        ///   Linear coefficient (Intercept).
        /// </summary>
        public double Intercept
        {
            get { return regression.Coefficients[0]; }
        }


        /// <summary>
        ///   Performs the Simple Linear Regression.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="outputs">The output data.</param>
        /// <returns>The regression Sum-of-Squares error.</returns>
        public double Regress(double[] inputs, double[] outputs)
        {
            double[][] X = new double[inputs.Length][];

            for (int i = 0; i < inputs.Length; i++)
            {
                // b[0]*1 + b[1]*inputs[i]
                X[i] = new double[] { 1.0, inputs[i] };
            }

            return regression.Regress(X, outputs);
        }

        /// <summary>
        ///   Computes the regression
        /// </summary>
        /// <param name="input">An array of input values.</param>
        /// <returns>The array of calculated output values.</returns>
        public double[] Compute(double[] input)
        {
            double[] output = new double[input.Length];

            // Call Compute(v) for each input vector v
            for (int i = 0; i < input.Length; i++)
                output[i] = Compute(input[i]);

            return output;
        }

        /// <summary>
        ///   Computes the regression for a single input.
        /// </summary>
        /// <param name="input">The input value.</param>
        /// <returns>The calculated output.</returns>
        public double Compute(double input)
        {
            return Slope * input + Intercept;
        }

        /// <summary>
        ///   Gets the coefficient of determination, or R^2
        /// </summary>
        /// <remarks>
        ///    The coefficient of determination is used in the context of statistical models
        ///    whose main purpose is the prediction of future outcomes on the basis of other
        ///    related information. It is the proportion of variability in a data set that
        ///    is accounted for by the statistical model. It provides a measure of how well
        ///    future outcomes are likely to be predicted by the model.
        ///    
        ///    The R^2 coefficient of determination is a statistical measure of how well the
        ///    regression line approximates the real data points. An R^2 of 1.0 indicates
        ///    that the regression line perfectly fits the data.
        /// </remarks>
        /// <returns></returns>
        public double CoefficientOfDetermination(double[] inputs, double[] outputs, bool adjust)
        {
            double[][] X = new double[inputs.Length][];

            for (int i = 0; i < inputs.Length; i++)
            {
                // b[0]*1 + b[1]*inputs[i]
                X[i] = new double[] { 1.0, inputs[i] };
            }

            return regression.CoefficientOfDetermination(X,outputs,adjust);
        }

                 /// <summary>
        ///   Gets the coefficient of determination, or R^2
        /// </summary>
        /// <remarks>
        ///    The coefficient of determination is used in the context of statistical models
        ///    whose main purpose is the prediction of future outcomes on the basis of other
        ///    related information. It is the proportion of variability in a data set that
        ///    is accounted for by the statistical model. It provides a measure of how well
        ///    future outcomes are likely to be predicted by the model.
        ///    
        ///    The R^2 coefficient of determination is a statistical measure of how well the
        ///    regression line approximates the real data points. An R^2 of 1.0 indicates
        ///    that the regression line perfectly fits the data.
        /// </remarks>
        /// <returns></returns>
        public double CoefficientOfDetermination(double[] inputs, double[] outputs)
        {
            return CoefficientOfDetermination(inputs, outputs, false);
        }

        /// <summary>
        ///   Returns a System.String representing the regression.
        /// </summary>
        public override string ToString()
        {
            return String.Format("y(x) = {0}x + {1}", Slope, Intercept);
        }
    }
}
