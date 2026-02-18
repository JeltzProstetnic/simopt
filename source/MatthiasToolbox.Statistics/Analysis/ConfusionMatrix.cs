using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Utilities;

namespace MatthiasToolbox.Statistics.Analysis
{
    /// <summary>
    /// A class for a 2x2 confusion matrix.
    /// TODO: extend to make this work for NxN
    /// </summary>
    public class ConfusionMatrix
    {
        #region cvar

        private int truePositives;
        private int trueNegatives;
        private int falsePositives;
        private int falseNegatives;

        #endregion
        #region prop

        #region set

        public double FalsePositiveCost { get; set; }
        public double FalseNegativeCost { get; set; }

        #endregion
        #region basic

        /// <summary>
        /// The cases which were correctly identified as positives.
        /// </summary>
        public int TruePositives
        {
            get { return truePositives; }
        }

        /// <summary>
        /// The cases which were correctly identified as negatives.
        /// </summary>
        public int TrueNegatives
        {
            get { return trueNegatives; }
        }

        /// <summary>
        /// The cases which were incorrectly identified as positives.
        /// </summary>
        public int FalsePositives
        {
            get { return falsePositives; }
        }

        /// <summary>
        /// The cases which were incorrectly identified as negatives.
        /// </summary>
        public int FalseNegatives
        {
            get { return falseNegatives; }
        }

        /// <summary>
        /// The total number of observations for this matrix. (true negatives + 
        /// true positives + false negatives + false positives)
        /// </summary>
        public int Observations
        {
            get
            {
                return trueNegatives + truePositives + falseNegatives + falsePositives;
            }
        }

        /// <summary>
        /// The number of actual positives (true positives + false negatives).
        /// </summary>
        public int ActualPositives
        {
            get { return truePositives + falseNegatives; }
        }

        /// <summary>
        /// The number of actual negatives (true negatives + false positives).
        /// </summary>
        public int ActualNegatives
        {
            get { return trueNegatives + falsePositives; }
        }

        /// <summary>
        /// The number of predicted positives (true positives + false positives).
        /// </summary>
        public int PredictedPositives
        {
            get { return truePositives + falsePositives; }
        }

        /// <summary>
        /// The number of predicted negatives (true negatives + false negatives).
        /// </summary>
        public int PredictedNegatives
        {
            get { return trueNegatives + falseNegatives; }
        }

        #endregion
        #region derived

        /// <summary>
        /// The True Positive Rate TPR = TP / (TP + FN)
        /// </summary>
        public double Sensitivity
        {
            get { return (double)truePositives / (double)(truePositives + falseNegatives); }
        }

        /// <summary>
        /// The True Negative Rate TNR = TN / (FP + TN)
        /// </summary>
        public double Specificity
        {
            get { return (double)trueNegatives / (double)(trueNegatives + falsePositives); }
        }

        /// <summary>
        /// The arithmetic mean of sensitivity and specificity.
        /// </summary>
        public double Efficiency
        {
            get { return (Sensitivity + Specificity) / 2d; }
        }

        /// <summary>
        /// The performance of the system ACC = (TP + TN) / (P + N)
        /// </summary>
        public double Accuracy
        {
            get
            {
                return ((double)truePositives + (double)trueNegatives) / (double)Observations;
            }
        }

        /// <summary>
        /// Positive Precision PPV = TP / (TP + FP). Calculates how likely it is that 
        /// the observed instance is positive given that the test is positive. (0..1)
        /// </summary>
        public double PositivePredictiveValue
        {
            get
            {
                if (PredictedPositives != 0) 
                    return (double)truePositives / (double)PredictedPositives;
                else
                    return 1d;
            }
        }

        /// <summary>
        /// Negative Precision NPV = TN / (TN + FN). Calculates how likely it is that 
        /// the observed instance is negative given that the test is negative. (0..1)
        /// </summary>
        public double NegativePredictiveValue
        {
            get
            {
                if (PredictedNegatives != 0)
                    return (double)trueNegatives / (double)PredictedNegatives;
                else
                    return 1d;
            }
        }

        /// <summary>
        /// The expected false positive rate FDR = FP / (FP + TP)
        /// </summary>
        public double FalseDiscoveryRate
        {
            get
            {
                if (PredictedPositives != 0)
                    return (double)falsePositives / (double)PredictedPositives;
                else
                    return 1d;
            }
        }

        /// <summary>
        /// Also known as false alarm rate. FPR = FP / (FP + TN)
        /// </summary>
        public double FalsePositiveRate
        {
            get
            {
                return (double)falsePositives / (double)(falsePositives + trueNegatives);
            }
        }

        /// <summary>
        /// Phi coefficient. A coefficient of +1 represents a perfect prediction, 
        /// 0 an average random prediction and −1 an inverse prediction.
        /// </summary>
        public double MatthewsCorrelationCoefficient
        {
            get
            {
                int tmp = (truePositives + falsePositives) *
                    (truePositives + falseNegatives) *
                    (trueNegatives + falsePositives) *
                    (trueNegatives + falseNegatives);

                double s = System.Math.Sqrt(tmp);

                if (tmp != 0) return (double)((truePositives * trueNegatives) - (falsePositives * falseNegatives)) / s;
                else return 0d;
            }
        }

        ///// <summary>
        ///// The average expected cost of classification at point x,y in the ROC space C = 1 - ((1-p) alpha x + p beta (1-y))
        ///// alpha = cost of a false positive (false alarm) 
        ///// beta = cost of missing a positive (false negative) 
        ///// p = proportion of positive cases
        ///// </summary>
        //public double WeighedCost {
        //    get
        //    {
        //        return 1 - ((1 - ActualPositives / Observations) * FalsePositiveCost * FalsePositiveRate
        //            + (ActualPositives / Observations) * FalseNegativeCost * (1 - Sensitivity));
        //    }
        //}

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// Create a new instance.
        /// </summary>
        public ConfusionMatrix(int truePositives, int trueNegatives, int falsePositives, int falseNegatives)
        {
            this.truePositives = truePositives;
            this.trueNegatives = trueNegatives;
            this.falsePositives = falsePositives;
            this.falseNegatives = falseNegatives;
            this.FalseNegativeCost = 1;
            this.FalsePositiveCost = 1;
        }

        /// <summary>
        /// Create a new instance from boolean arrays.
        /// </summary>
        /// <param name="predicted">The values as predicted by the model.</param>
        /// <param name="expected">The actual values.</param>
        public ConfusionMatrix(IEnumerable<bool> predicted, IEnumerable<bool> expected)
        {
            IEnumerator<bool> expectedEnumerator = expected.GetEnumerator();
            
            MultiEnumerator<bool> e = new MultiEnumerator<bool>(predicted, expected);

            foreach(bool currentPredicted in predicted)
            {
                expectedEnumerator.MoveNext();
                bool currentExpected = expectedEnumerator.Current;
                bool prediction = currentPredicted;
                bool expectation = currentExpected;

                if (expectation == prediction)
                {
                    if (prediction) truePositives++;
                    else trueNegatives++;
                }
                else
                {
                    if (prediction == true) falsePositives++;
                    else falseNegatives++;
                }
            }
        }

        public static ConfusionMatrix Create<T>(IEnumerable<T> predicted, IEnumerable<T> expected, Predicate<T> isPositive)
        {
            int truePositives = 0;
            int trueNegatives = 0;
            int falsePositives = 0;
            int falseNegatives = 0;

            foreach (List<T> item in new MultiEnumerable<T>(predicted, expected))
            {
                bool prediction = isPositive.Invoke(item[0]);
                bool expectation = isPositive.Invoke(item[1]);

                if (expectation == prediction)
                {
                    if (prediction == true) truePositives++;
                    else trueNegatives++;
                }
                else
                {
                    if (prediction == true) falsePositives++;
                    else falseNegatives++;
                }
            }

            return new ConfusionMatrix(truePositives, trueNegatives, falsePositives, falseNegatives);
        }

        public ConfusionMatrix(int[] predicted, int[] expected, int positiveValue)
        {
            for (int i = 0; i < predicted.Length; i++)
            {
                bool prediction = predicted[i] == positiveValue;
                bool expectation = expected[i] == positiveValue;

                if (expectation == prediction)
                {
                    if (prediction) truePositives++;
                    else trueNegatives++;
                }
                else
                {
                    if (prediction) falsePositives++;
                    else falseNegatives++;
                }
            }
        }

        public ConfusionMatrix(int[] predicted, int[] expected, int positiveValue, int negativeValue)
        {
            for (int i = 0; i < predicted.Length; i++)
            {
                if (predicted[i] == expected[i])
                {
                    if (predicted[i] == positiveValue)
                        truePositives++; // Positive hit
                    else if (predicted[i] == negativeValue)
                        trueNegatives++; // Negative hit
                }
                else
                {
                    if (predicted[i] == positiveValue)
                        falsePositives++; // Positive miss
                    else if (predicted[i] == negativeValue)
                        falseNegatives++; // Negative miss
                }
            }

        }

        #endregion
    }
}