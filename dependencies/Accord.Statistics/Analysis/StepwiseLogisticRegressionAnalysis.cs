// Accord Statistics Library
// Accord.NET framework
// http://www.crsouza.com
//
// Copyright © César Souza, 2009-2010
// cesarsouza at gmail.com
//


using Accord.Math;
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Testing;
using System.Collections.ObjectModel;
using System;

namespace Accord.Statistics.Analysis
{

    /// <summary>
    ///   Backward Stepwise Logistic Regression Analysis
    /// </summary>
    /// <remarks>
    ///   The Backward Stepwise regression is an exploratory analysis procedure,
    ///   where the analysis begins with a full (saturated) model and at each step
    ///   variables are eliminated from the model in a iterative fashion.
    ///   
    ///   Significance tests are performed after each removal to track which of
    ///   the variables can be discarded safely without implying in degradation.
    ///   
    ///   When no more variables can be removed from the model without causing
    ///   a significative loss in the model likelihood, the method can stop.
    /// </remarks>
    public class StepwiseLogisticRegressionAnalysis
    {


        private double[][] inputData;
        private double[] outputData;

        private string[] inputNames;
        private string outputName;


        private Model currentModel;
        private ModelCollection nestedModelCollection;
        private double fullLikelihood;

        private double threshold = 0.15;

        // Fitting parameters
        private int maxIterations = 100;
        private double limit = 10e-4;


        //---------------------------------------------


        #region Constructors
        /// <summary>
        ///   Constructs a Stepwise Logistic Regression Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output data for the analysis.</param>
        public StepwiseLogisticRegressionAnalysis(double[][] inputs, double[] outputs, String[] inputNames, String outputName)
        {
            this.inputData = inputs;
            this.outputData = outputs;

            this.inputNames = inputNames;
            this.outputName = outputName;
        }
        #endregion


        //---------------------------------------------


        #region Properties
        /// <summary>
        ///   Gets the current nested model.
        /// </summary>
        public Model Current
        {
            get { return this.currentModel; }
        }

        /// <summary>
        ///   Gets the collection of nested models obtained after 
        ///   a step of the backward stepwise procedure.
        /// </summary>
        public ModelCollection Nested
        {
            get { return nestedModelCollection; }
        }

        /// <summary>
        ///   Gets the name of the input variables.
        /// </summary>
        public String[] Inputs
        {
            get { return this.inputNames; }
        }

        /// <summary>
        ///   Gets the name of the output variables.
        /// </summary>
        public String Output
        {
            get { return this.outputName; }
        }

        /// <summary>
        ///   Gets or sets the significance threshold used to
        ///   determine if a nested model is significant or not.
        /// </summary>
        public double Threshold
        {
            get { return threshold; }
            set { threshold = value; }
        }
        #endregion


        //---------------------------------------------


        /// <summary>
        ///   Computes the Stepwise Logistic Regression.
        /// </summary>
        /// <returns>
        ///   Returns the final set of input variables indices
        ///   selected by the stepwise procedure.
        /// </returns>
        public int[] Compute()
        {
            int changed;
            do
            {
                changed = DoStep();

            } while (changed != -1);

            return currentModel.Variables;
        }

        /// <summary>
        ///   Computes one step of the Stepwise Logistic Regression Analysis.
        /// </summary>
        /// <returns>
        ///   Returns the index of the variable discarded in the step or -1
        ///   in case no variable could be discarded.
        /// </returns>
        public int DoStep()
        {
            // Check if we are performing the first step
            if (currentModel == null)
            {
                // This is the first step. We should create the full model.
                int inputCount = inputData[0].Length;
                LogisticRegression regression = new LogisticRegression(inputCount);
                int[] variables = Matrix.Indices(0, inputCount);
                fit(regression, inputData, outputData);
                ChiSquareTest test = regression.ChiSquare(inputData, outputData);
                fullLikelihood = regression.GetLogLikelihood(inputData, outputData);

                if (Double.IsNaN(fullLikelihood))
                    throw new Exception("Perfect separation detected. Please rethink the use of logistic regression.");

                currentModel = new Model(this, regression, variables, test);
            }


            // Verify first if a variable reduction is possible
            if (currentModel.Regression.Inputs == 1)
                return -1; // cannot reduce further


            // Now go and create the diminished nested models
            Model[] nestedModels = new Model[currentModel.Regression.Inputs];
            for (int i = 0; i < nestedModels.Length; i++)
            {
                // Create a diminished nested model without the current variable
                LogisticRegression regression = new LogisticRegression(currentModel.Regression.Inputs - 1);
                int[] variables = currentModel.Variables.RemoveAt(i);
                double[][] subset = inputData.Submatrix(0, inputData.Length - 1, variables);
                fit(regression, subset, outputData);

                // Check the significance of the nested model
                double logLikelihood = regression.GetLogLikelihood(subset, outputData);
                double ratio = 2.0 * (fullLikelihood - logLikelihood);
                ChiSquareTest test = new ChiSquareTest(ratio, inputNames.Length-variables.Length, threshold);

                // Store the nested model
                nestedModels[i] = new Model(this, regression, variables, test);
            }

            // Select the model with the highest p-value
            double pmax = 0; int imax = -1;
            for (int i = 0; i < nestedModels.Length; i++)
            {
                if (nestedModels[i].ChiSquare.PValue >= pmax)
                {
                    imax = i;
                    pmax = nestedModels[i].ChiSquare.PValue;
                }
            }

            // Create the read-only nested model collection
            this.nestedModelCollection = new ModelCollection(nestedModels);


            // If the model with highest p-value is not significant,
            if (imax >= 0 && pmax > threshold)
            {
                // Then this means the variable can be safely discarded from the full model
                int removed = currentModel.Variables[imax];

                // Our diminished nested model will become our next full model.
                this.currentModel = nestedModels[imax];

                // Finally, return the index of the removed variable
                return removed;
            }
            else
            {
                // Else we can not safely remove any variable from the model.
                return -1;
            }
        }


        /// <summary>
        ///   Fits a logistic regression model to data until convergence.
        /// </summary>
        private bool fit(LogisticRegression regression, double[][] input, double[] output)
        {
            double delta;
            int iteration = 0;

            do // learning iterations until convergence
            {
                delta = regression.Regress(input, output);
                iteration++;

            } while (delta > limit && iteration < maxIterations);

            // Check if the full model has converged
            return iteration <= maxIterations;
        }


        /// <summary>
        ///   Logistic Regression Model
        /// </summary>
        public class Model
        {
            /// <summary>
            ///   Gets the Stepwise Logistic Regression Analysis
            ///   from which this model belongs to.
            /// </summary>
            public StepwiseLogisticRegressionAnalysis Analysis { get; private set; }

            /// <summary>
            ///   Gets the regression model.
            /// </summary>
            public LogisticRegression Regression { get; private set; }

            /// <summary>
            ///   Gets the subset of the original variables used by the model.
            /// </summary>
            public int[] Variables { get; private set; }

            /// <summary>
            ///   Gets the Chi-Square Likelihood Ratio test for the model.
            /// </summary>
            public ChiSquareTest ChiSquare { get; private set; }

            /// <summary>
            ///   Gets the subset of the original variables used by the model.
            /// </summary>
            public string[] Names
            {
                get { return Analysis.inputNames.Submatrix(Variables); }
            }

            /// <summary>
            ///   Constructs a new Logistic regression model.
            /// </summary>
            internal Model(StepwiseLogisticRegressionAnalysis analysis, LogisticRegression regression,
                int[] variables, ChiSquareTest test)
            {
                this.Analysis = analysis;
                this.Regression = regression;
                this.Variables = variables;
                this.ChiSquare = test;
            }

        }

        /// <summary>
        ///   Model collection. This class cannot be instantiated outside
        ///   a Stepwise Logistic Regression Analysis.
        /// </summary>
        public class ModelCollection : ReadOnlyCollection<Model>
        {
            internal ModelCollection(Model[] models)
                : base(models)
            {
            }
        }
    }
}
