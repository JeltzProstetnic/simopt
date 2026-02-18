using System;
using System.Collections.ObjectModel;
using SimOpt.Mathematics;
using SimOpt.Mathematics.Numerics.Decompositions;

namespace SimOpt.Statistics.Analysis
{

    /// <summary>
    /// Determines the method to be used in a Component Analysis.
    /// </summary>
    public enum AnalysisMethod
    {
        /// <summary>
        ///  By choosing Center, the method will be run on the mean-centered data.
        ///  In Principal Component Analysis this means the method will operate
        ///  on the Covariance matrix of the given data.
        /// </summary>
        Center,

        /// <summary>
        ///  By choosing Standardize, the method will be run on the mean-centered and
        ///  standardized data. In Principal Component Analysis this means the method
        ///  will operate on the Correlation matrix of the given data. One should always
        ///  choose to standardize when dealing with different units of variables.
        /// </summary>
        Standardize,
    };

    /// <summary>
    ///   Principal component analysis (PCA) is a technique used to reduce
    ///   multidimensional data sets to lower dimensions for analysis.
    /// </summary>
    /// <remarks>
    ///   Principal Components Analysis or the Karhunen-Loeve expansion is a
    ///   classical method for dimensionality reduction or exploratory data
    ///   analysis.
    ///  
    ///   Mathematically, PCA is a process that decomposes the covariance matrix of a matrix
    ///   into two parts: eigenvalues and column eigenvectors, whereas Singular Value Decomposition
    ///   (SVD) decomposes a matrix per se into three parts: singular values, column eigenvectors,
    ///   and row eigenvectors. The relationships between PCA and SVD lie in that the eigenvalues 
    ///   are the square of the singular values and the column vectors are the same for both.   
    ///   
    ///   This class uses SVD on the data set which generally gives better numerical accuracy.
    ///</remarks>
    public class PrincipalComponentAnalysis
    {

        private double[,] sourceMatrix;
        private double[,] resultMatrix;

        private double[] columnMeans;
        private double[] columnStdDev;

        private double[,] eigenVectors;
        private double[] eigenValues;
        private double[] singularValues;

        private PrincipalComponentCollection componentCollection;
        private double[] componentProportions;
        private double[] componentCumulative;

        private AnalysisMethod analysisMethod;

        //---------------------------------------------


        #region Constructor
        /// <summary>Constructs a new Principal Component Analysis.</summary>
        /// <param name="data">The source data to perform analysis. The matrix should contain
        /// variables as columns and observations of each variable as rows.</param>
        /// <param name="method">The analysis method to perform.</param>
        public PrincipalComponentAnalysis(double[,] data, AnalysisMethod method)
        {
            this.sourceMatrix = data;
            this.analysisMethod = method;

            // Calculate common measures to speedup other calculations
            this.columnMeans = MMath.Mean(sourceMatrix);
            this.columnStdDev = MMath.StandardDeviation(sourceMatrix, columnMeans);

        }

        /// <summary>Constructs a new Principal Component Analysis.</summary>
        /// <param name="data">The source data to perform analysis. The matrix should contain
        /// variables as columns and observations of each variable as rows.</param>
        public PrincipalComponentAnalysis(double[,] data)
            : this(data, AnalysisMethod.Center)
        {
        }
        #endregion


        //---------------------------------------------


        #region Properties
        /// <summary>Returns the original data supplied to the analysis.</summary>
        public double[,] Source
        {
            get { return this.sourceMatrix; }
        }

        /// <summary>Gets the resulting projection of the source data given on the creation of the analysis into an orthogonal space.</summary>
        public double[,] Result
        {
            get { return this.resultMatrix; }
            protected set { this.resultMatrix = value; }
        }

        /// <summary>Gets the matrix whose columns contain the principal components. Also known as the Eigenvectors or FeatureVectors matrix.</summary>
        public double[,] ComponentMatrix
        {
            get { return this.eigenVectors; }
            protected set { this.eigenVectors = value; }
        }

        /// <summary>Gets the Principal Components in a object-oriented fashion.</summary>
        public PrincipalComponentCollection Components
        {
            get { return componentCollection; }
        }

        /// <summary>The respective role each component plays in the data set.</summary>
        public double[] ComponentProportions
        {
            get { return componentProportions; }
        }

        /// <summary>The cumulative distribution of the components proportion role. Also known
        /// as the cumulative energy of the principal components.</summary>
        public double[] CumulativeProportions
        {
            get { return componentCumulative; }
        }

        /// <summary>Provides access to the Singular Values stored during the analysis.
        /// If a covariance method is choosen, then it will contain an empty vector.</summary>
        public double[] SingularValues
        {
            get { return singularValues; }
            protected set { singularValues = value; }
        }

        /// <summary>Provides access to the Eigen Values stored during the analysis.</summary>
        public double[] Eigenvalues
        {
            get { return eigenValues; }
            protected set { eigenValues = value; }
        }

        /// <summary>
        ///   Gets the column standard deviations of the source data given at method construction.
        /// </summary>
        public double[] StandardDeviations
        {
            get { return this.columnStdDev; }
        }

        /// <summary>
        ///   Gets the column mean of the source data given at method construction.
        /// </summary>
        public double[] Means
        {
            get { return this.columnMeans; }
        }

        /// <summary>Gets or sets the method used by this analysis.</summary>
        public AnalysisMethod Method
        {
            get { return this.analysisMethod; }
            set { this.analysisMethod = value; }
        }
        #endregion


        //---------------------------------------------


        #region Public Methods

        /// <summary>Computes the Principal Component Analysis algorithm.</summary>
        public virtual void Compute()
        {
            int rows = sourceMatrix.GetLength(0);
            int cols = sourceMatrix.GetLength(1);

            // Center and standardize the source matrix
            double[,] matrix = adjust(sourceMatrix);


            // Perform the Singular Value Decomposition (SVD) of the matrix
            SingularValueDecomposition svd = new SingularValueDecomposition(matrix);
            singularValues = svd.Diagonal;

            //  The principal components of 'Source' are the eigenvectors of Cov(Source). Thus if we
            //  calculate the SVD of 'matrix' (which is Source standardized), the columns of matrix V
            //  (right side of SVD) will be the principal components of Source.                        

            // The right singular vectors contains the principal components of the data matrix
            this.eigenVectors = svd.RightSingularVectors;

            // The left singular vectors contains the scores of the principal components
            this.resultMatrix = svd.LeftSingularVectors;


            // Eigen values are the square of the singular values
            eigenValues = new double[singularValues.Length];
            for (int i = 0; i < singularValues.Length; i++)
            {
                eigenValues[i] = singularValues[i] * singularValues[i];
            }

            // Computes additional information about the analysis and creates the
            //  object-oriented structure to hold the principal components found.
            createComponents();
        }

        /// <summary>Projects a given matrix into principal component space.</summary>
        /// <param name="matrix">The matrix to be projected.</param>
        public double[,] Transform(double[,] matrix)
        {
            return this.Transform(matrix, componentCollection.Count);
        }

        /// <summary>Projects a given matrix into principal component space.</summary>
        /// <param name="matrix">The matrix to be projected.</param>
        /// <param name="components">The number of components to consider.</param>
        public virtual double[,] Transform(double[,] matrix, int components)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double[,] r = new double[rows, components];
            double[,] s = adjust(matrix);


            // multiply the data matrix by the selected eigenvectors
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < components; j++)
                    for (int k = 0; k < cols; k++)
                        r[i, j] += s[i, k] * eigenVectors[k, j];


            return r;
        }


        /// <summary>
        ///   Reverts a set of projected data into it's original form. Complete reverse
        ///   transformation is only possible if all components are present, and, if the
        ///   data has been standardized, the original standard deviation and means of
        ///   the original matrix are known.
        /// </summary>
        /// <param name="data">The pca transformed data.</param>
        public virtual double[,] Revert(double[,] data)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);
            int components = componentCollection.Count;
            double[,] reversion = new double[rows, components];


            // Revert the data (reversion = data * eigenVectors.Transpose())
            for (int i = 0; i < components; i++)
                for (int j = 0; j < rows; j++)
                    for (int k = 0; k < cols; k++)
                        reversion[j, i] += data[j, k] * eigenVectors[i, k];


            // if the data has been standardized or centered,
            //  we need to revert those operations as well
            if (this.analysisMethod == AnalysisMethod.Standardize)
            {
                // multiply by standard deviation and add the mean
                for (int i = 0; i < rows; i++)
                    for (int j = 0; j < components; j++)
                        reversion[i, j] = (reversion[i, j] * columnStdDev[j]) + columnMeans[j];
            }
            else
            {
                // only add the mean
                for (int i = 0; i < rows; i++)
                    for (int j = 0; j < components; j++)
                        reversion[i, j] = reversion[i, j] + columnMeans[j];
            }

            return reversion;
        }


        /// <summary>
        ///   Returns the minimal number of principal components
        ///   required to represent a given percentile of the data.
        /// </summary>
        /// <param name="threshold">The percentile of the data requiring representation.</param>
        /// <returns>The minimal number of components required.</returns>
        public int GetNumberOfComponents(float threshold)
        {
            if (threshold < 0 || threshold > 1.0)
                throw new ArgumentException("Threshold should be a value between 0 and 1", "threshold");

            for (int i = 0; i < componentCumulative.Length; i++)
            {
                if (componentCumulative[i] >= threshold)
                    return i+1;
            }

            return componentCumulative.Length;
        }

        #endregion


        //---------------------------------------------


        #region Protected Methods
        /// <summary>
        ///   Adjusts a data matrix, centering and standardizing its values
        ///   using the already computed column's means and standard deviations.
        /// </summary>
        protected double[,] adjust(double[,] m)
        {
            int rows = m.GetLength(0);
            int cols = m.GetLength(1);
            double[,] matrix = new double[rows, cols];

            // Prepare the data, storing it in the new matrix.
            if (this.analysisMethod == AnalysisMethod.Standardize)
            {
                for (int i = 0; i < rows; i++)
                    for (int j = 0; j < cols; j++)
                        // subtract mean and divide by standard deviation (convert to Z Scores)
                        matrix[i, j] = (m[i, j] - columnMeans[j]) / columnStdDev[j];
            }
            else
            {
                for (int i = 0; i < rows; i++)
                    for (int j = 0; j < cols; j++)
                        // Just center the data around the mean. Will have no effect if the
                        //  data is already centered (the mean will be zero).
                        matrix[i, j] = (m[i, j] - columnMeans[j]);
            }

            return matrix;
        }

        /// <summary>
        ///   Creates additional information about principal components.
        /// </summary>
        protected void createComponents()
        {
            int numComponents = singularValues.Length;
            componentProportions = new double[numComponents];
            componentCumulative = new double[numComponents];

            // Calculate proportions
            double sum = 0.0;
            for (int i = 0; i < numComponents; i++)
                sum += System.Math.Abs(eigenValues[i]);
            sum = (sum == 0) ? 0.0 : (1.0 / sum);

            for (int i = 0; i < numComponents; i++)
                componentProportions[i] = System.Math.Abs(eigenValues[i]) * sum;

            // Calculate cumulative proportions
            this.componentCumulative[0] = this.componentProportions[0];
            for (int i = 1; i < this.componentCumulative.Length; i++)
                this.componentCumulative[i] = this.componentCumulative[i - 1] + this.componentProportions[i];

            // Creates the object-oriented structure to hold the principal components
            PrincipalComponent[] components = new PrincipalComponent[singularValues.Length];
            for (int i = 0; i < components.Length; i++)
                components[i] = new PrincipalComponent(this, i);
            this.componentCollection = new PrincipalComponentCollection(components);
        }
        #endregion

    }


    #region Support Classes
    /// <summary>
    ///   Represents a Principal Component found in the Principal Component Analysis,
    ///   allowing it to be bound to controls like the DataGridView. This class cannot
    ///   be instantiated outside the PrincipalComponentAnalysis.
    /// </summary>
    public class PrincipalComponent
    {

        private int index;
        private PrincipalComponentAnalysis principalComponentAnalysis;


        /// <summary>
        ///   Creates a principal component representation.
        /// </summary>
        /// <param name="analysis">The analysis to which this component belongs.</param>
        /// <param name="index">The component index.</param>
        internal PrincipalComponent(PrincipalComponentAnalysis analysis, int index)
        {
            this.index = index;
            this.principalComponentAnalysis = analysis;
        }


        /// <summary>Gets the Index of this component on the original analysis principal component collection.</summary>
        public int Index
        {
            get { return this.index; }
        }

        /// <summary>Returns a reference to the parent analysis object.</summary>
        public PrincipalComponentAnalysis Analysis
        {
            get { return this.principalComponentAnalysis; }
        }

        /// <summary>Gets the proportion of data this component represents.</summary>
        public double Proportion
        {
            get { return this.principalComponentAnalysis.ComponentProportions[index]; }
        }

        /// <summary>Gets the cumulative proportion of data this component represents.</summary>
        public double CumulativeProportion
        {
            get { return this.principalComponentAnalysis.CumulativeProportions[index]; }
        }

        /// <summary>If available, gets the Singular Value of this component found during the Analysis.</summary>
        public double SingularValue
        {
            get { return this.principalComponentAnalysis.SingularValues[index]; }
        }

        /// <summary>Gets the Eigenvalue of this component found during the analysis.</summary>
        public double Eigenvalue
        {
            get { return this.principalComponentAnalysis.Eigenvalues[index]; }
        }

        /// <summary>Gets the Eigenvector of this component.</summary>
        public double[] Eigenvector
        {
            get
            {
                double[] eigv = new double[principalComponentAnalysis.ComponentMatrix.GetLength(0)];

                for (int i = 0; i < eigv.Length; i++)
                    eigv[i] = principalComponentAnalysis.ComponentMatrix[i, index];
                return eigv;
            }
        }
    }

    /// <summary>
    ///   Represents a Collection of Principal Components found in the Principal Component Analysis.
    ///   This class cannot be instantiated.
    /// </summary>
    public class PrincipalComponentCollection : ReadOnlyCollection<PrincipalComponent>
    {
        internal PrincipalComponentCollection(PrincipalComponent[] components)
            : base(components)
        {
        }
    }
    #endregion

}
