// Accord Statistics Library
// Accord.NET framework
// http://www.crsouza.com
//
// Copyright © César Souza, 2009
// cesarsouza at gmail.com
//

using System;
using System.Linq;
using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Statistics.Testing;
using AForge;

namespace Accord.Statistics.Models.Regression
{

    /// <summary>
    ///   Logistic Regression
    /// </summary>
    /// <remarks>
    ///   In statistics, logistic regression (sometimes called the logistic model or
    ///   logit model) is used for prediction of the probability of occurrence of an
    ///   event by fitting data to a logistic curve. It is a generalized linear model
    ///   used for binomial regression. Like many forms of regression analysis, it
    ///   makes use of several predictor variables that may be either numerical or
    ///   categorical. For example, the probability that a person has a heart attack within a
    ///   specified time period might be predicted from knowledge of the person's age,
    ///   sex and body mass index.
    ///   
    ///   Logistic regression is used extensively in the medical and social sciences
    ///   as well as marketing applications such as prediction of a customer's
    ///   propensity to purchase a product or cease a subscription.
    /// 
    ///   References:
    ///    - http://www.cs.cmu.edu/~ggordon/IRLS-example/logistic.m
    ///    - http://userwww.sfsu.edu/~efc/classes/biol710/logistic/logisticreg.htm
    ///    - http://www.stat.cmu.edu/~cshalizi/350/lectures/26/lecture-26.pdf
    ///    - http://www.inf.ed.ac.uk/teaching/courses/lfd/lectures/logisticlearn-print.pdf
    ///    
    /// </remarks>
    public class LogisticRegression : ICloneable
    {

        private double[] coefficients;
        private double[] standardErrors;


        //---------------------------------------------


        #region Constructor
        /// <summary>
        ///   Creates a new Logistic Regression Model.
        /// </summary>
        /// <param name="inputs">The number of input variables for the model.</param>
        public LogisticRegression(int inputs)
        {
            this.coefficients = new double[inputs + 1];
            this.standardErrors = new double[inputs + 1];
        }

        /// <summary>
        ///   Creates a new Logistic Regression Model.
        /// </summary>
        /// <param name="inputs">The number of input variables for the model.</param>
        /// <param name="intercept">The starting intercept value.</param>
        public LogisticRegression(int inputs, double intercept)
            : this(inputs)
        {
            this.coefficients[0] = intercept;
        }
        #endregion


        //---------------------------------------------


        #region Properties
        /// <summary>
        ///   Gets the coefficient vector, with its first
        ///   value being the intercept value.
        /// </summary>
        public double[] Coefficients
        {
            get { return coefficients; }
        }

        /// <summary>
        ///   Gets the number of inputs handled by this model.
        /// </summary>
        public int Inputs
        {
            get { return coefficients.Length - 1; }
        }
        #endregion


        //---------------------------------------------


        #region Public Methods
        /// <summary>
        ///   Computes the model output for the given input vector.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <returns>The output value.</returns>
        public double Compute(double[] input)
        {
            double logit = coefficients[0];

            for (int i = 1; i < coefficients.Length; i++)
                logit += input[i - 1] * coefficients[i];

            return Logistic(logit);
        }

        /// <summary>
        ///   Computes the model output for each of the given input vectors.
        /// </summary>
        /// <param name="input">The array of input vectors.</param>
        /// <returns>The array of output values.</returns>
        public double[] Compute(double[][] input)
        {
            double[] output = new double[input.Length];

            for (int i = 0; i < input.Length; i++)
                output[i] = Compute(input[i]);

            return output;
        }


        /// <summary>
        ///   Gets the Odds Ratio for a given coefficient.
        /// </summary>
        /// <remarks>
        ///   The odds ratio can be computed raising euler's number
        ///   (e ~~ 2.71) to the power of the associated coefficient.
        /// </remarks>
        /// <param name="index">
        ///   The coefficient's index. The first value
        ///   (at zero index) is the intercept value.
        /// </param>
        /// <returns>
        ///   The Odds Ratio for the given coefficient.
        /// </returns>
        public double GetOddsRatio(int index)
        {
            return System.Math.Exp(coefficients[index]);
        }

        /// <summary>
        ///   Gets the Standard Error for a given coefficient.
        /// </summary>
        /// <param name="index">
        ///   The coefficient's index. The first value
        ///   (at zero index) is the intercept value.
        /// </param>
        /// <returns>
        ///   The Standard Error for the given coefficient.
        /// </returns>
        public double GetStandardError(int index)
        {
            return standardErrors[index];
        }

        /// <summary>
        ///   Gets the 95% confidence interval for the
        ///   Odds Ratio for a given coefficient.
        /// </summary>
        /// <param name="index">
        ///   The coefficient's index. The first value
        ///   (at zero index) is the intercept value.
        /// </param>
        public DoubleRange GetConfidenceInterval(int index)
        {
            double coeff = coefficients[index];
            double error = standardErrors[index];

            double upper = coeff + 1.96 * error;
            double lower = coeff - 1.96 * error;

            DoubleRange ci = new DoubleRange(
               System.Math.Exp(lower),
               System.Math.Exp(upper));

            return ci;

        }

        /// <summary>
        ///   Gets the Wald Test for a given coefficient.
        /// </summary>
        /// <remarks>
        ///   See also http://en.wikipedia.org/wiki/Wald_test
        /// </remarks>
        /// <param name="index">
        ///   The coefficient's index. The first value
        ///   (at zero index) is the intercept value.
        /// </param>
        public WaldTest GetWaldTest(int index)
        {
            return new WaldTest(coefficients[index], 0.0, standardErrors[index]);
        }


        /// <summary>
        ///   Gets the Log-Likelihood for the model.
        /// </summary>
        /// <param name="input">A set of input data.</param>
        /// <param name="output">A set of output data.</param>
        /// <returns>
        ///   The Log-Likelihood (a measure of performance) of
        ///   the model calculated over the given data sets.
        /// </returns>
        public double GetLogLikelihood(double[][] input, double[] output)
        {
            // The logarithm of sums equals the summation of logs
            // Equivalent to Sum(output[i]*ln(y)+(1-output[i])*ln(1-y))

            double sum = 0.0, y;
            for (int i = 0; i < input.Length; i++)
            {
                y = this.Compute(input[i]);

                sum += (output[i] != 0.0) ?
                    System.Math.Log(y) :
                    System.Math.Log(1.0 - y);
            }

            return sum;
        }

        /// <summary>
        ///   Gets the Deviance for the model
        /// </summary>
        /// <remarks>
        ///   The deviance is defined as -2*Log-Likelihood.
        /// </remarks>
        /// <param name="input">A set of input data.</param>
        /// <param name="output">A set of output data.</param>
        /// <returns>
        ///   The deviance (a measure of performance) of the model
        ///   calculated over the given data sets.
        /// </returns>
        public double GetDeviance(double[][] input, double[] output)
        {
            return -2.0 * GetLogLikelihood(input, output);
        }

        /// <summary>
        ///   Gets the Likelihood Ratio between two models.
        /// </summary>
        /// <remarks>
        ///   The deviance is defined as -2*Log-Likelihood.
        /// </remarks>
        /// <param name="input">A set of input data.</param>
        /// <param name="output">A set of output data.</param>
        /// <param name="regression">Another Logistic Regression model.</param>
        /// <returns>The Log-Likelihood ratio (a measure of performance
        /// between two models) calculated over the given data sets.</returns>
        public double GetLogLikelihoodRatio(double[][] input, double[] output, LogisticRegression regression)
        {
            return 2.0 * (this.GetLogLikelihood(input, output) - regression.GetLogLikelihood(input, output));
        }


        /// <summary>
        ///   The likelihood ratio test of the overall model, also called the model chi-square test.
        /// </summary>
        /// <remarks>
        ///   The likelihood ratio test, also called the log-likelihood test, is based on
        ///   -2LL (deviance). The likelihood ratio test is a test of the significance of
        ///   the difference between the likelihood ratio (-2LL) for the researcher's model
        ///   minus the likelihood ratio for a reduced model. This difference is called the
        ///   "model chi-square."
        ///   
        ///   The likelihood ratio test is generally preferred over its alternative, the Wald test,
        ///   discussed below.
        /// </remarks>
        public ChiSquareTest ChiSquare(double[][] input, double[] output)
        {
            double y0 = output.Count(y => y == 0.0);
            double y1 = output.Length - y0;

            LogisticRegression regression = new LogisticRegression(coefficients.Length - 1, System.Math.Log(y1 / y0));

            double ratio = GetLogLikelihoodRatio(input, output, regression);
            return new ChiSquareTest(ratio, coefficients.Length - 1);
        }


        /// <summary>
        ///   Iterates one pass of the optimization algorithm trying to find
        ///   the best regression coefficients for the logistic model.
        /// </summary>
        /// <remarks>
        ///   An iterative Newton-Raphson algorithm is used to calculate
        ///   the maximum likelihood values of the parameters.  This procedure
        ///   uses the partial second derivatives of the parameters in the
        ///   Hessian matrix to guide incremental parameter changes in an effort
        ///   to maximize the log likelihood value for the likelihood function. 
        /// </remarks>
        /// <returns>
        ///   The absolute value of the largest parameter change.
        /// </returns>
        public double Regress(double[][] input, double[] output)
        {
            // Regress using Iterative Reweighted Least Squares estimation.

            // Initial definitions and memory allocations
            int N = input.Length;
            int M = this.Coefficients.Length;
            double[,] regression = new double[N, M];
            double[,] hessian = new double[M, M];
            double[] gradient = new double[M];
            double[] errors = new double[N];
            double[] R = new double[N];
            double[] deltas;


            // Compute the regression matrix, errors and diagonal
            for (int i = 0; i < N; i++)
            {
                double y = this.Compute(input[i]);
                double o = output[i];

                // Calculate error vector
                errors[i] = y - o;

                // Calculate R diagonal
                R[i] = y * (1.0 - y);

                // Compute the regression matrix
                regression[i, 0] = 1;
                for (int j = 1; j < M; j++)
                    regression[i, j] = input[i][j - 1];
            }


            // Compute error gradient and "Hessian" matrix (with diagonal R)
            for (int i = 0; i < M; i++)
            {
                // Compute error gradient
                for (int j = 0; j < N; j++)
                    gradient[i] += regression[j, i] * errors[j];

                // Compute "Hessian" matrix (regression'*R*regression)
                for (int j = 0; j < M; j++)
                    for (int k = 0; k < N; k++)
                        hessian[j, i] += regression[k, i] * (R[k] * regression[k, j]);
            }


            // Decompose to solve the linear system
            LuDecomposition lu = new LuDecomposition(hessian);
            double[,] inverse;

            if (lu.NonSingular)
            {
                // Solve using LU decomposition
                deltas = lu.Solve(gradient);
                inverse = lu.Inverse();
            }
            else
            {
                // Hessian Matrix is singular, try pseudo-inverse solution
                SingularValueDecomposition svd = new SingularValueDecomposition(hessian);
                deltas = svd.Solve(gradient);
                inverse = svd.Inverse();
            }

            // Update coefficients using the calculated deltas
            for (int i = 0; i < coefficients.Length; i++)
                this.coefficients[i] -= deltas[i];

            // Calculate Coefficients standard errors
            for (int i = 0; i < standardErrors.Length; i++)
                standardErrors[i] = System.Math.Sqrt(inverse[i, i]);


            // Return the absolute value of the largest parameter change
            return Matrix.Max(Matrix.Abs(deltas));
        }


        /// <summary>
        ///   Creates a new LogisticRegression that is a copy of the current instance.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            LogisticRegression regression = new LogisticRegression(coefficients.Length);
            regression.coefficients = (double[])this.coefficients.Clone();
            regression.standardErrors = (double[])this.standardErrors.Clone();
            return regression;
        }
        #endregion


        //---------------------------------------------


        #region Static Methods
        /// <summary>
        ///   The Logistic function.
        /// </summary>
        /// <param name="z">The logit parameter.</param>
        public static double Logistic(double z)
        {
            return 1.0 / (1.0 + System.Math.Exp(-z));
        }
        #endregion

    }

}
