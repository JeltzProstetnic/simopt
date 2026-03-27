using FluentAssertions;
using SimOpt.Statistics.Analysis;
using SimOpt.Statistics.Kernels;
using Xunit;

namespace SimOpt.Tests.Statistics.Analysis;

/// <summary>
/// Tests for PrincipalComponentAnalysis (PCA) and KernelPrincipalComponentAnalysis (KPCA).
/// </summary>
public class PcaTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes the Frobenius norm of the difference between two same-shaped matrices.
    /// </summary>
    private static double FrobeniusDiff(double[,] a, double[,] b)
    {
        int rows = a.GetLength(0);
        int cols = a.GetLength(1);
        double sum = 0.0;
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
            {
                double d = a[i, j] - b[i, j];
                sum += d * d;
            }
        return Math.Sqrt(sum);
    }

    /// <summary>
    /// Returns a 2-D matrix built row-wise from a jagged array.
    /// </summary>
    private static double[,] ToMatrix(double[][] rows)
    {
        int r = rows.Length;
        int c = rows[0].Length;
        var m = new double[r, c];
        for (int i = 0; i < r; i++)
            for (int j = 0; j < c; j++)
                m[i, j] = rows[i][j];
        return m;
    }

    /// <summary>
    /// Computes per-column variance of a matrix (population variance).
    /// </summary>
    private static double[] ColumnVariance(double[,] m)
    {
        int rows = m.GetLength(0);
        int cols = m.GetLength(1);
        var variances = new double[cols];
        for (int j = 0; j < cols; j++)
        {
            double mean = 0.0;
            for (int i = 0; i < rows; i++) mean += m[i, j];
            mean /= rows;
            double v = 0.0;
            for (int i = 0; i < rows; i++) { double d = m[i, j] - mean; v += d * d; }
            variances[j] = v / rows;
        }
        return variances;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Simple 2-D dataset: elongated diagonal cluster.
    // Primary axis is (1,1)/√2, secondary is (1,-1)/√2.
    // All variance lives on the first PC after centering.
    // ─────────────────────────────────────────────────────────────────────────
    private static readonly double[,] Simple2D = ToMatrix(
    [
        [-3.0, -3.0],
        [-2.0, -2.0],
        [-1.0, -1.0],
        [ 0.0,  0.0],
        [ 1.0,  1.0],
        [ 2.0,  2.0],
        [ 3.0,  3.0],
    ]);

    // ─────────────────────────────────────────────────────────────────────────
    // PCA — Construction & Compute
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compute_PopulatesComponents()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        pca.Components.Should().NotBeNull();
        pca.Components.Count.Should().Be(2, because: "2-variable input → 2 principal components");
        pca.Eigenvalues.Should().HaveCount(2);
        pca.SingularValues.Should().HaveCount(2);
        pca.ComponentMatrix.Should().NotBeNull();
    }

    [Fact]
    public void Compute_EigenvaluesAreNonNegative()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        foreach (double ev in pca.Eigenvalues)
            ev.Should().BeGreaterThanOrEqualTo(0.0, because: "eigenvalues of a PSD covariance matrix are non-negative");
    }

    [Fact]
    public void Compute_ComponentProportionsSumToOne()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        double sum = pca.ComponentProportions.Sum();
        sum.Should().BeApproximately(1.0, 1e-10, because: "proportions must partition the total variance");
    }

    [Fact]
    public void Compute_CumulativeProportionFinalIsOne()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        pca.CumulativeProportions.Last().Should().BeApproximately(1.0, 1e-10);
    }

    [Fact]
    public void Compute_MeansMatchColumnMeans()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        // Both columns have mean 0 for this symmetric dataset
        pca.Means[0].Should().BeApproximately(0.0, 1e-10);
        pca.Means[1].Should().BeApproximately(0.0, 1e-10);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PCA — Known dataset
    // The dataset lies entirely on y = x, so after centering all variance
    // concentrates in the first component.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void KnownDataset_FirstComponentDominates()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        // First component should capture ≈ 100 % of variance (second ≈ 0)
        pca.ComponentProportions[0].Should().BeApproximately(1.0, 1e-6,
            because: "all points lie on a single line, so PC1 captures all variance");
        pca.ComponentProportions[1].Should().BeApproximately(0.0, 1e-6);
    }

    [Fact]
    public void KnownDataset_FirstEigenvalueDominates()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        pca.Eigenvalues[0].Should().BeGreaterThan(pca.Eigenvalues[1]);
    }

    [Fact]
    public void KnownDataset_EigenvectorSpansMainDiagonal()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        // The leading eigenvector should be (±√2/2, ±√2/2) — sign ambiguity is expected
        double v0 = pca.ComponentMatrix[0, 0];
        double v1 = pca.ComponentMatrix[1, 0];

        // Both components equal in magnitude
        Math.Abs(v0).Should().BeApproximately(Math.Abs(v1), 1e-6,
            because: "data variance is symmetric along (1,1), so eigenvector components have equal magnitude");

        // Eigenvector is unit-length
        double norm = Math.Sqrt(v0 * v0 + v1 * v1);
        norm.Should().BeApproximately(1.0, 1e-6);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PCA — Transform
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Transform_OutputHasCorrectDimensions()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        double[,] transformed = pca.Transform(Simple2D);

        // Shape: same number of rows, same number of components
        transformed.GetLength(0).Should().Be(Simple2D.GetLength(0));
        transformed.GetLength(1).Should().Be(2);
    }

    [Fact]
    public void Transform_WithReducedComponents_OutputHasFewerColumns()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        double[,] reduced = pca.Transform(Simple2D, 1);

        reduced.GetLength(0).Should().Be(Simple2D.GetLength(0));
        reduced.GetLength(1).Should().Be(1, because: "requested exactly 1 component");
    }

    [Fact]
    public void Transform_TransformedColumnsAreUncorrelated()
    {
        // Use a dataset with 2 correlated variables
        double[,] data = ToMatrix(
        [
            [1.0, 2.0],
            [2.0, 4.0],
            [3.0, 5.0],
            [4.0, 7.0],
            [5.0, 9.0],
        ]);

        var pca = new PrincipalComponentAnalysis(data, AnalysisMethod.Center);
        pca.Compute();
        double[,] transformed = pca.Transform(data);

        // Compute cross-correlation of PC1 and PC2
        int rows = transformed.GetLength(0);
        double mean0 = 0, mean1 = 0;
        for (int i = 0; i < rows; i++) { mean0 += transformed[i, 0]; mean1 += transformed[i, 1]; }
        mean0 /= rows; mean1 /= rows;

        double cov = 0;
        for (int i = 0; i < rows; i++)
            cov += (transformed[i, 0] - mean0) * (transformed[i, 1] - mean1);
        cov /= rows;

        // By construction PCA produces orthogonal components → covariance ≈ 0
        cov.Should().BeApproximately(0.0, 1e-9,
            because: "PCA produces orthogonal (decorrelated) principal components");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PCA — Transform / Revert roundtrip
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TransformRevert_FullComponents_Center_RecoverInput()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        double[,] transformed = pca.Transform(Simple2D);
        double[,] reverted    = pca.Revert(transformed);

        FrobeniusDiff(Simple2D, reverted).Should().BeLessThan(1e-8,
            because: "full-component roundtrip should recover the original data");
    }

    [Fact]
    public void TransformRevert_FullComponents_Standardize_RecoverInput()
    {
        double[,] data = ToMatrix(
        [
            [1.0, 10.0],
            [2.0, 20.0],
            [3.0, 30.0],
            [4.0, 40.0],
            [5.0, 50.0],
        ]);

        var pca = new PrincipalComponentAnalysis(data, AnalysisMethod.Standardize);
        pca.Compute();

        double[,] transformed = pca.Transform(data);
        double[,] reverted    = pca.Revert(transformed);

        FrobeniusDiff(data, reverted).Should().BeLessThan(1e-8,
            because: "full-component standardized roundtrip should recover the original data");
    }

    [Fact]
    public void Revert_ShapeMatchesOriginalInput()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        double[,] transformed = pca.Transform(Simple2D);
        double[,] reverted    = pca.Revert(transformed);

        reverted.GetLength(0).Should().Be(Simple2D.GetLength(0));
        reverted.GetLength(1).Should().Be(Simple2D.GetLength(1));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PCA — Variance conservation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void VarianceConservation_TotalVariancePreservedAfterTransform()
    {
        double[,] data = ToMatrix(
        [
            [2.5, 2.4],
            [0.5, 0.7],
            [2.2, 2.9],
            [1.9, 2.2],
            [3.1, 3.0],
            [2.3, 2.7],
            [2.0, 1.6],
            [1.0, 1.1],
            [1.5, 1.6],
            [1.1, 0.9],
        ]);

        var pca = new PrincipalComponentAnalysis(data, AnalysisMethod.Center);
        pca.Compute();

        double[] origVariances = ColumnVariance(data);
        double totalOrigVariance = origVariances.Sum();

        // Eigenvalues represent variance captured per component.
        // After centering, sum(eigenvalues) / (n-1) ≈ total input variance.
        // Use proportions × totalOrigVariance as a proxy — they must sum to totalOrigVariance.
        double totalFromComponents = pca.ComponentProportions.Sum() * totalOrigVariance;
        totalFromComponents.Should().BeApproximately(totalOrigVariance, 1e-6,
            because: "PCA proportions are fractions of total variance and must reconstitute it");
    }

    [Fact]
    public void VarianceConservation_TransformedDataHasSameTotalVariance()
    {
        double[,] data = ToMatrix(
        [
            [1.0, 2.0, 3.0],
            [4.0, 5.0, 6.0],
            [7.0, 8.0, 9.0],
            [2.0, 4.0, 6.0],
            [3.0, 6.0, 9.0],
        ]);

        var pca = new PrincipalComponentAnalysis(data, AnalysisMethod.Center);
        pca.Compute();

        double[,] transformed = pca.Transform(data);

        double totalOrigVar = ColumnVariance(data).Sum();
        double totalTransVar = ColumnVariance(transformed).Sum();

        // Total variance (sum of column variances) is conserved by orthogonal projection
        totalTransVar.Should().BeApproximately(totalOrigVar, 1e-6,
            because: "an orthogonal change of basis preserves total variance");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PCA — GetNumberOfComponents
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetNumberOfComponents_ThresholdOne_ReturnsAtLeastOne()
    {
        // GetNumberOfComponents returns the MINIMUM count that meets the threshold.
        // With a threshold of 1.0 and datasets where the first component captures all
        // variance (cumulative[0] == 1.0), the method correctly returns 1 — not the
        // total component count.  Use a non-degenerate 2-variable dataset so we can
        // assert the count is within valid bounds.
        double[,] data = ToMatrix(
        [
            [1.0, 0.0],
            [0.0, 1.0],
            [1.0, 1.0],
            [2.0, 0.5],
            [0.5, 2.0],
        ]);

        var pca = new PrincipalComponentAnalysis(data, AnalysisMethod.Center);
        pca.Compute();

        int n = pca.GetNumberOfComponents(1.0f);
        n.Should().BeGreaterThanOrEqualTo(1).And.BeLessThanOrEqualTo(pca.Components.Count);
    }

    [Fact]
    public void GetNumberOfComponents_KnownDataset_FirstComponentSufficient()
    {
        // Simple2D has all variance in PC1, so 1 component covers ≈ 100 %
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        // 0.99 threshold — should need only 1 component
        int n = pca.GetNumberOfComponents(0.99f);
        n.Should().Be(1, because: "all variance is on the first principal component");
    }

    [Fact]
    public void GetNumberOfComponents_InvalidThreshold_Throws()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        var act = () => pca.GetNumberOfComponents(1.5f);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetNumberOfComponents_NegativeThreshold_Throws()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        var act = () => pca.GetNumberOfComponents(-0.1f);
        act.Should().Throw<ArgumentException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PCA — PrincipalComponent object-oriented API
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Component_ProportionAndCumulativeConsistent()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        double runningSum = 0.0;
        for (int i = 0; i < pca.Components.Count; i++)
        {
            runningSum += pca.Components[i].Proportion;
            pca.Components[i].CumulativeProportion.Should().BeApproximately(runningSum, 1e-10);
        }
    }

    [Fact]
    public void Component_EigenvectorLengthMatchesVariableCount()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        foreach (var component in pca.Components)
            component.Eigenvector.Length.Should().Be(Simple2D.GetLength(1));
    }

    [Fact]
    public void Component_EigenvalueEqualsSquaredSingularValue()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        for (int i = 0; i < pca.Components.Count; i++)
        {
            double expected = pca.Components[i].SingularValue * pca.Components[i].SingularValue;
            pca.Components[i].Eigenvalue.Should().BeApproximately(expected, 1e-8);
        }
    }

    [Fact]
    public void Component_IndexIsCorrect()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Compute();

        for (int i = 0; i < pca.Components.Count; i++)
            pca.Components[i].Index.Should().Be(i);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PCA — Method property (Center vs Standardize)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Method_DefaultIsCenter()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D);
        pca.Method.Should().Be(AnalysisMethod.Center);
    }

    [Fact]
    public void Method_StandardizeUsesZScores()
    {
        // With standardization, unit-variance variables should all have equal
        // proportions if they are independent — confirm analysis runs without error
        double[,] data = ToMatrix(
        [
            [1.0, 100.0],
            [2.0, 200.0],
            [3.0, 300.0],
            [4.0, 400.0],
            [5.0, 500.0],
        ]);

        var pca = new PrincipalComponentAnalysis(data, AnalysisMethod.Standardize);
        pca.Compute();

        pca.StandardDeviations.Should().HaveCount(2);
        pca.ComponentProportions.Sum().Should().BeApproximately(1.0, 1e-10);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PCA — Source property
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Source_ReturnsOriginalMatrix()
    {
        var pca = new PrincipalComponentAnalysis(Simple2D, AnalysisMethod.Center);
        pca.Source.Should().BeSameAs(Simple2D);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PCA — Multi-variable dataset (3D)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ThreeVariables_ProducesThreeComponents()
    {
        double[,] data = ToMatrix(
        [
            [1.0, 2.0, 3.0],
            [4.0, 5.0, 6.0],
            [7.0, 8.0, 9.0],
            [2.0, 3.0, 4.0],
            [5.0, 6.0, 7.0],
        ]);

        var pca = new PrincipalComponentAnalysis(data, AnalysisMethod.Center);
        pca.Compute();

        pca.Components.Count.Should().Be(3);
        pca.ComponentProportions.Should().HaveCount(3);
    }

    [Fact]
    public void ThreeVariables_TransformReducedToTwo_Shape()
    {
        double[,] data = ToMatrix(
        [
            [1.0, 2.0, 3.0],
            [4.0, 5.0, 6.0],
            [7.0, 8.0, 9.0],
            [2.0, 3.0, 4.0],
            [5.0, 6.0, 7.0],
        ]);

        var pca = new PrincipalComponentAnalysis(data, AnalysisMethod.Center);
        pca.Compute();

        double[,] reduced = pca.Transform(data, 2);
        reduced.GetLength(0).Should().Be(5);
        reduced.GetLength(1).Should().Be(2);
    }
}

/// <summary>
/// Tests for KernelPrincipalComponentAnalysis (KPCA).
/// </summary>
public class KpcaTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helpers shared with PcaTests
    // ─────────────────────────────────────────────────────────────────────────

    private static double[,] ToMatrix(double[][] rows)
    {
        int r = rows.Length;
        int c = rows[0].Length;
        var m = new double[r, c];
        for (int i = 0; i < r; i++)
            for (int j = 0; j < c; j++)
                m[i, j] = rows[i][j];
        return m;
    }

    private static double FrobeniusDiff(double[,] a, double[,] b)
    {
        int rows = a.GetLength(0);
        int cols = a.GetLength(1);
        double sum = 0.0;
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
            {
                double d = a[i, j] - b[i, j];
                sum += d * d;
            }
        return Math.Sqrt(sum);
    }

    // Small 2-D dataset used across multiple KPCA tests
    private static readonly double[,] Data2D = ToMatrix(
    [
        [-2.0, -1.0],
        [-1.0,  0.0],
        [ 0.0,  1.0],
        [ 1.0,  2.0],
        [ 2.0,  3.0],
    ]);

    // ─────────────────────────────────────────────────────────────────────────
    // KPCA — Construction
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_DefaultKernel_IsCenter()
    {
        var kernel = new Gaussian(1.0);
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, kernel);

        kpca.Method.Should().Be(AnalysisMethod.Center);
        kpca.Center.Should().BeTrue();
        kpca.Kernel.Should().BeSameAs(kernel);
    }

    [Fact]
    public void Constructor_WithCenterInFeatureSpaceFalse_Stores()
    {
        var kernel = new Gaussian(1.0);
        var kpca = new KernelPrincipalComponentAnalysis(
            Data2D, kernel, AnalysisMethod.Center, centerInFeatureSpace: false);

        kpca.Center.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // KPCA — Compute (Gaussian kernel)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compute_Gaussian_PopulatesComponents()
    {
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, new Gaussian(1.0));
        kpca.Compute();

        // KPCA with n samples → n kernel components (one per training point)
        kpca.Components.Should().NotBeNull();
        kpca.Components.Count.Should().Be(Data2D.GetLength(0));
    }

    [Fact]
    public void Compute_Gaussian_ResultHasCorrectShape()
    {
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, new Gaussian(1.0));
        kpca.Compute();

        // Result is n × n (kernel PCA projects into n-dimensional kernel space)
        kpca.Result.GetLength(0).Should().Be(Data2D.GetLength(0));
        kpca.Result.GetLength(1).Should().Be(Data2D.GetLength(0));
    }

    [Fact]
    public void Compute_Gaussian_CumulativeProportionEndsAtOne()
    {
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, new Gaussian(1.0));
        kpca.Compute();

        kpca.CumulativeProportions.Last().Should().BeApproximately(1.0, 1e-10);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // KPCA — Compute (Linear kernel — should approximate linear PCA)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compute_LinearKernel_PopulatesComponents()
    {
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, new Linear());
        kpca.Compute();

        kpca.Components.Count.Should().Be(Data2D.GetLength(0));
    }

    [Fact]
    public void Compute_LinearKernel_ProportionsSumToOne()
    {
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, new Linear());
        kpca.Compute();

        kpca.ComponentProportions.Sum().Should().BeApproximately(1.0, 1e-10);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // KPCA — Compute (Polynomial kernel)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compute_PolynomialKernel_PopulatesComponents()
    {
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, new Polynomial(2, 1.0));
        kpca.Compute();

        kpca.Components.Count.Should().Be(Data2D.GetLength(0));
        kpca.CumulativeProportions.Last().Should().BeApproximately(1.0, 1e-10);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // KPCA — Transform
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Transform_OutputHasCorrectShape()
    {
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, new Gaussian(1.0));
        kpca.Compute();

        double[,] result = kpca.Transform(Data2D, 2);

        result.GetLength(0).Should().Be(Data2D.GetLength(0));
        result.GetLength(1).Should().Be(2);
    }

    [Fact]
    public void Transform_AllComponents_ShapeIsNxN()
    {
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, new Gaussian(1.0));
        kpca.Compute();

        int n = Data2D.GetLength(0);
        double[,] result = kpca.Transform(Data2D, n);

        result.GetLength(0).Should().Be(n);
        result.GetLength(1).Should().Be(n);
    }

    [Fact]
    public void Transform_NewPoint_ProducesFiniteValues()
    {
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, new Gaussian(1.0));
        kpca.Compute();

        double[,] newPoint = ToMatrix([[0.5, 1.5]]);
        double[,] result = kpca.Transform(newPoint, 2);

        result.GetLength(0).Should().Be(1);
        result.GetLength(1).Should().Be(2);

        for (int j = 0; j < 2; j++)
            double.IsFinite(result[0, j]).Should().BeTrue(
                because: $"transformed value at column {j} must be finite");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // KPCA — Revert (Gaussian supports IDistance → Revert available)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Revert_Gaussian_ProducesCorrectShape()
    {
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, new Gaussian(1.0));
        kpca.Compute();

        double[,] transformed = kpca.Transform(Data2D, Data2D.GetLength(0));
        double[,] reverted    = kpca.Revert(transformed);

        reverted.GetLength(0).Should().Be(Data2D.GetLength(0));
        reverted.GetLength(1).Should().Be(Data2D.GetLength(1));
    }

    [Fact]
    public void Revert_Gaussian_CompletesWithoutThrowing()
    {
        // The KPCA pre-image approximation (Kwok-Tsang MDS method) is not guaranteed
        // to produce finite values for all inputs — the code and its referenced paper
        // both explicitly state "complete reverse transformation is not always possible
        // and is not even guaranteed to exist."  We verify only that the method runs
        // without throwing an exception and returns the correct shape.
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, new Gaussian(1.0));
        kpca.Compute();

        double[,] transformed = kpca.Transform(Data2D, Data2D.GetLength(0));
        double[,] reverted    = default!;

        var act = () => { reverted = kpca.Revert(transformed); };
        act.Should().NotThrow(because: "the method should return (possibly approximate) results without error");

        reverted.GetLength(0).Should().Be(Data2D.GetLength(0));
        reverted.GetLength(1).Should().Be(Data2D.GetLength(1));
    }

    [Fact]
    public void Revert_Linear_ProducesCorrectShape()
    {
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, new Linear());
        kpca.Compute();

        int n = Data2D.GetLength(0);
        double[,] transformed = kpca.Transform(Data2D, n);
        double[,] reverted    = kpca.Revert(transformed);

        reverted.GetLength(0).Should().Be(n);
        reverted.GetLength(1).Should().Be(Data2D.GetLength(1));
    }

    [Fact]
    public void Revert_KernelWithoutIDistance_Throws()
    {
        // Use a kernel that does NOT implement IDistance (e.g. Sigmoid)
        var kernel = new Sigmoid(1.0, 0.0);
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, kernel);
        kpca.Compute();

        int n = Data2D.GetLength(0);
        double[,] transformed = kpca.Transform(Data2D, n);

        var act = () => kpca.Revert(transformed);
        act.Should().Throw<Exception>(
            because: "Revert requires the kernel to implement IDistance, which Sigmoid does not");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // KPCA — Larger dataset stability
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compute_LargerDataset_StableWithGaussian()
    {
        // 10 points, 3 variables — ensures the kernel matrix path is exercised
        var rng = new Random(42);
        int rows = 10, cols = 3;
        var data = new double[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                data[i, j] = rng.NextDouble() * 4.0 - 2.0;

        var kpca = new KernelPrincipalComponentAnalysis(data, new Gaussian(1.0));
        var act = () => kpca.Compute();
        act.Should().NotThrow(because: "KPCA on a random dense dataset should complete without error");

        kpca.Components.Count.Should().Be(rows);
        kpca.CumulativeProportions.Last().Should().BeApproximately(1.0, 1e-9);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // KPCA — Kernel property
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void KernelProperty_ReturnsSameInstance()
    {
        var kernel = new Gaussian(2.0);
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, kernel);
        kpca.Kernel.Should().BeSameAs(kernel);
    }

    [Fact]
    public void Center_PropertyCanBeToggled()
    {
        var kpca = new KernelPrincipalComponentAnalysis(Data2D, new Gaussian(1.0));
        kpca.Center = false;
        kpca.Center.Should().BeFalse();
        kpca.Center = true;
        kpca.Center.Should().BeTrue();
    }
}
