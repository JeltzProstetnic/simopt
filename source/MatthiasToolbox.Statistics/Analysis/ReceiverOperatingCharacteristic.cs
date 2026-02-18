using System;
using System.Collections.Generic;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Statistics.Analysis
{
    /// <summary>
    /// Receiver Operating Characteristic (ROC) Curve, 
    /// a graphical plot of the sensitivity vs. (1 − specificity) for a 
    /// binary classifier system as its discrimination threshold is varied.
    /// The area under the ROC curve is computed directly using the trapezoidal rule.
    /// </summary>
    public class ReceiverOperatingCharacteristic<T>
    {
        #region dele

        public delegate bool ClassificationDelegate(T instance, double threshold);
        public delegate bool VerificationDelegate(T instance, bool classificationResult);

        #endregion
        #region cvar

        private int? actualPositives;
        private int? actualNegatives;

        private double? area;
        private double? error; 
        
        #endregion
        #region prop

        public ConfusionMatrix BestPoint { get; private set; }
        public double BestThreshold { get; private set; }
        public List<ConfusionMatrix> Points { get; private set; }
        public IEnumerable<T> Instances { get; set; }
        public ClassificationDelegate Classificator { get; set; }
        public VerificationDelegate Verificator { get; set; }
        //public double FalsePositiveCost { get; set; }
        //public double FalseNegativeCost { get; set; }
        public bool PointsCalculated { get; private set; }

        /// <summary>
        /// Calculate the area under the ROC curve using the trapezium method.
        /// If the result is &lt; 0.5 1-result will be returned.
        /// </summary>
        public double Area
        {
            get
            {
                if (!PointsCalculated) 
                    throw new ArgumentNullException("The ROC instance must have its points calculated before the area under the curve can be evaluated.");
                
                if (!area.HasValue)
                {
                    double sum = 0d;
                    double trapez = 0d;

                    for (int i = 0; i < Points.Count - 1; i++)
                    {
                        trapez = Points[i].Sensitivity + Points[i + 1].Sensitivity;
                        trapez *= (Points[i].FalsePositiveRate - Points[i + 1].FalsePositiveRate) / 2d;
                        sum += trapez;
                    }

                    if (sum < 0.5d) area = 1d - sum;
                    else area = sum;
                }

                return area.Value;
            }
        }

        /// <summary>
        /// Calculate the standard error associated with this ROC curve.
        /// </summary>
        public double Error
        {
            get
            {
                if (!error.HasValue)
                {
                    double A = Area;
                    int Na = actualPositives.Value;
                    int Nn = actualNegatives.Value;

                    double Q1 = A / (2.0 - A);
                    double Q2 = 2 * A * A / (1.0 + A);

                    error = System.Math.Sqrt((A * (1.0 - A) +
                        (Na - 1.0) * (Q1 - A * A) +
                        (Nn - 1.0) * (Q2 - A * A)) / (Na * Nn));
                }

                return error.Value;
            }
        }

        #endregion
        #region ctor

        public ReceiverOperatingCharacteristic(
            IEnumerable<T> instances = null, 
            ClassificationDelegate classificator = null, 
            VerificationDelegate verificator = null)
        {
            this.Instances = instances;
            this.Classificator = classificator;
            this.Verificator = verificator;
            //this.FalseNegativeCost = 1;
            //this.FalsePositiveCost = 1;
        }

        #endregion
        #region impl

        public void Calculate(int resolution)
        {
            Calculate(1d / (double)resolution);   
        }

        public void Calculate(double increment)
        {
            Points = new List<ConfusionMatrix>();

            int tp; int fp;
            int tn; int fn;

            double x = 0d;
            double bestPhi = 0;

            while (x < 1d)
            {
                tp = 0;
                fp = 0;
                tn = 0;
                fn = 0;

                foreach (T instance in Instances)
                {
                    if (Classificator.Invoke(instance, x))  // classified as positive
                    {
                        if (Verificator.Invoke(instance, true)) tp++;
                        else fp++;
                    }
                    else                                    // classified as negative
                    {
                        if (Verificator.Invoke(instance, false)) tn++;
                        else fn++;
                    }
                }

                if (!actualNegatives.HasValue)
                {
                    actualNegatives = tn + fp;
                    actualPositives = tp + fn;
                }

                ConfusionMatrix point = new ConfusionMatrix(tp, tn, fp, fn);

                // if (point.MatthewsCorrelationCoefficient > bestPhi)
                if (point.Accuracy > bestPhi)
                {
                    // bestPhi = point.MatthewsCorrelationCoefficient;
                    bestPhi = point.Accuracy;
                    BestPoint = point;
                    BestThreshold = x;
                }

                Points.Add(point);

                x += increment;
            }

            //points.Sort(new Comparison<ConfusionMatrix>((a, b) => b.Specificity.CompareTo(a.Specificity)));
            //Points.Sort(new Comparison<ConfusionMatrix>((a, b) => a.FalsePositiveRate.CompareTo(b.FalsePositiveRate)));
            PointsCalculated = true;
        }

        /// <summary>
        /// Compare two ROC curves.
        /// </summary>
        /// <param name="r">The amount of correlation between the two curves</param>
        /// <returns>ΔA / (SE² + other.SE² - 2 * r * SE * other.SE) (SE = standard error, ΔA = area - other.area)</returns>
        public double Compare(ReceiverOperatingCharacteristic<T> other, double r)
        {
            if (!PointsCalculated || !other.PointsCalculated) 
                throw new ArgumentNullException("Both ROC instances must have their points calculated to be compared.");

            double me = Error;
            double oe = other.Error;

            return (Area - other.Area) /
                System.Math.Sqrt(me * me + oe * oe - 2d * r * me * oe);
        }

        #endregion
        #region rem

        //private double area = 0.0;
        //private double error = 0.0;

        //// The actual, measured data
        //private double[] measurement;

        //// The data, as predicted by a test
        //private double[] prediction;


        //// The real number of positives and negatives in the measured (actual) data
        //private int positiveCount;
        //private int negativeCount;

        //// The values which represent positive and negative values in our
        ////  measurement data (such as presence or absence of some disease)
        //double dtrue;
        //double dfalse;

        //// The collection to hold our curve point information
        //private ReceiverOperatingCharacteristicPointCollection collection;

        ///// <summary>
        /////   Constructs a new Receiver Operating Characteristic model
        ///// </summary>
        ///// <param name="output">An array of binary values. Tipically 0 and 1, or -1 and 1, indicating negative and positive cases, respectively.</param>
        ///// <param name="predictedOutput">An array of continuous values trying to approximate the measurement array.</param>
        //public ReceiverOperatingCharacteristic(double[] measurement, double[] prediction)
        //{
        //    this.measurement = measurement;
        //    this.prediction = prediction;

        //    // Determine which numbers correspont to each binary category
        //    dtrue = dfalse = measurement[0];
        //    for (int i = 1; i < measurement.Length; i++)
        //    {
        //        if (dtrue < measurement[i])
        //            dtrue = measurement[i];
        //        if (dfalse > measurement[i])
        //            dfalse = measurement[i];
        //    }

        //    // Count the real number of positive and negative cases
        //    for (int i = 0; i < measurement.Length; i++)
        //    {
        //        if (measurement[i] == dtrue)
        //            this.positiveCount++;
        //    }

        //    // Negative cases is just the number of cases minus the number of positives
        //    this.negativeCount = this.measurement.Length - this.positiveCount;
        //}

        
        //#region Public Methods
        ///// <summary>
        /////   Computes a n-points ROC curve.
        ///// </summary>
        ///// <remarks>
        /////   Each point in the ROC curve will have a threshold increase of
        /////   1/npoints over the previous point, starting at zero.
        ///// </remarks>
        ///// <param name="points">The number of points for the curve.</param>
        //public void Compute(int points)
        //{
        //    Compute((dtrue - dfalse) / points);
        //}

        ///// <summary>
        /////   Computes a ROC curve with 1/increment points
        ///// </summary>
        ///// <param name="increment">The increment over the previous point for each point in the curve.</param>
        //public void Compute(double increment)
        //{
        //    double cutoff;
        //    List<ReceiverOperatingCharacteristicPoint> points = new List<ReceiverOperatingCharacteristicPoint>();

        //    // Create the curve, computing a point for each cutoff value
        //    for (cutoff = dfalse; cutoff <= dtrue; cutoff += increment) points.Add(ComputePoint(cutoff));
        //    if (cutoff < dtrue) points.Add(ComputePoint(dtrue));

        //    // Sort the curve by descending specificity
        //    points.Sort(new Comparison<ReceiverOperatingCharacteristicPoint>((a, b) => a.Specificity.CompareTo(b.Specificity)));

        //    // Create the point collection
        //    this.collection = new ReceiverOperatingCharacteristicPointCollection(points.ToArray());

        //    // Calculate area and error associated with this curve
        //    this.area = calculateAreaUnderCurve();
        //    this.error = calculateStandardError();
        //}

        ///// <summary>
        /////   Computes a ROC curve with 1/increment points
        ///// </summary>
        ///// <param name="increment">The increment over the previous point for each point in the curve.</param>
        //public void Compute(params double[] cutpoints)
        //{
        //    List<ReceiverOperatingCharacteristicPoint> points = new List<ReceiverOperatingCharacteristicPoint>();

        //    // Create the curve, computing a point for each cutpoint
        //    for (int i = 0; i < cutpoints.Length; i++) points.Add(ComputePoint(cutpoints[i]));

        //    // Sort the curve by descending specificity
        //    points.Sort(new Comparison<ReceiverOperatingCharacteristicPoint>((a, b) => a.Specificity.CompareTo(b.Specificity)));
            
        //    // Create the point collection
        //    this.collection = new ReceiverOperatingCharacteristicPointCollection(points.ToArray());

        //    // Calculate area and error associated with this curve
        //    this.area = calculateAreaUnderCurve();
        //    this.error = calculateStandardError();
        //}

        ///// <summary>
        /////   Computes a single point of a ROC curve using the given cutoff value.
        ///// </summary>
        //public ReceiverOperatingCharacteristicPoint ComputePoint(double threshold)
        //{
        //    int truePositives = 0;
        //    int trueNegatives = 0;

        //    for (int i = 0; i < this.measurement.Length; i++)
        //    {
        //        bool actual = (this.measurement[i] == dtrue);
        //        bool predicted = (this.prediction[i] >= threshold);


        //        // If the prediction equals the true measured value
        //        if (predicted == actual)
        //        {
        //            // We have a hit. Now we have to see
        //            //  if it was a positive or negative hit
        //            if (predicted == true)
        //                truePositives++; // Positive hit
        //            else trueNegatives++;// Negative hit
        //        }
        //    }

        //    // The other values can be computed from available variables
        //    int falsePositives = negativeCount - trueNegatives;
        //    int falseNegatives = positiveCount - truePositives;

        //    return new ReceiverOperatingCharacteristicPoint(this, threshold,
        //        truePositives, trueNegatives,
        //        falsePositives, falseNegatives);
        //}


        ///// <summary>
        /////   Compares two ROC curves.
        ///// </summary>
        ///// <param name="r">The amount of correlation between the two curves</param>
        ///// <returns></returns>
        //public double Compare(ReceiverOperatingCharacteristic curve, double r)
        //{
        //    // Areas
        //    double AUC1 = this.Area;
        //    double AUC2 = curve.Area;

        //    // Errors
        //    double se1 = this.Error;
        //    double se2 = curve.Error;

        //    // Standard error
        //    return (AUC1 - AUC2) / System.Math.Sqrt(se1 * se1 + se2 * se2 - 2 * r * se1 * se2);
        //}
        //#endregion


        //#region Private Methods
        ///// <summary>
        /////   Calculates the area under the ROC curve using the trapezium method.
        ///// </summary>
        ///// <remarks>
        /////   The area under a ROC curve can never be less than 0.50. If the area is first calculated as
        /////   less than 0.50, the definition of abnormal will be reversed from a higher test value to a
        /////   lower test value.
        ///// </remarks>
        //private double calculateAreaUnderCurve()
        //{
        //    double sum = 0.0;
        //    double tpz = 0.0;

        //    for (int i = 0; i < collection.Count - 1; i++)
        //    {
        //        // Obs: False Positive Rate = (1-specificity)
        //        tpz = collection[i].Sensitivity + collection[i + 1].Sensitivity;
        //        tpz = tpz * (collection[i].FalsePositiveRate - collection[i + 1].FalsePositiveRate) / 2.0;
        //        sum += tpz;
        //    }

        //    if (sum < 0.5)
        //        return 1.0 - sum;
        //    else return sum;
        //}

        ///// <summary>
        /////   Calculates the standard error associated with this curve
        ///// </summary>
        //private double calculateStandardError()
        //{
        //    double A = area;

        //    // real positive cases
        //    int Na = positiveCount;

        //    // real negative cases
        //    int Nn = negativeCount;

        //    double Q1 = A / (2.0 - A);
        //    double Q2 = 2 * A * A / (1.0 + A);

        //    return System.Math.Sqrt((A * (1.0 - A) +
        //        (Na - 1.0) * (Q1 - A * A) +
        //        (Nn - 1.0) * (Q2 - A * A)) / (Na * Nn));
        //}
        //#endregion
        ///// <summary>
        /////   Object to hold information about a Receiver Operating Characteristic Curve Point
        ///// </summary>
        //public class ReceiverOperatingCharacteristicPoint : ConfusionMatrix
        //{

        //    // Discrimination threshold (cutoff value)
        //    private double cutoff;

        //    // Parent curve
        //    ReceiverOperatingCharacteristic curve;

        //    /// <summary>
        //    ///   Constructs a new Receiver Operating Characteristic point.
        //    /// </summary>
        //    internal ReceiverOperatingCharacteristicPoint(ReceiverOperatingCharacteristic curve, double cutoff,
        //        int truePositives, int trueNegatives, int falsePositives, int falseNegatives)
        //        : base(truePositives, trueNegatives, falsePositives, falseNegatives)
        //    {
        //        this.curve = curve;
        //        this.cutoff = cutoff;
        //    }


        //    /// <summary>
        //    ///   Gets the cutoff value (discrimination threshold) for this point.
        //    /// </summary>
        //    public double Cutoff
        //    {
        //        get { return cutoff; }
        //    }
        //}

        ///// <summary>
        /////   Represents a Collection of Receiver Operating Characteristic (ROC) Curve points.
        /////   This class cannot be instantiated.
        ///// </summary>
        //public class ReceiverOperatingCharacteristicPointCollection : ReadOnlyCollection<ReceiverOperatingCharacteristicPoint>
        //{
        //    internal ReceiverOperatingCharacteristicPointCollection(ReceiverOperatingCharacteristicPoint[] points)
        //        : base(points)
        //    {
        //    }

        //}

        #endregion
    }
}