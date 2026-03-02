using FluentAssertions;
using SimOpt.Statistics.Analysis;
using Xunit;

namespace SimOpt.Tests.Statistics.Analysis;

public class ConfusionMatrixTests
{
    // Using known values: TP=90, TN=60, FP=10, FN=40 → 200 observations
    private readonly ConfusionMatrix _matrix = new(90, 60, 10, 40);

    [Fact]
    public void Constructor_StoresValues()
    {
        _matrix.TruePositives.Should().Be(90);
        _matrix.TrueNegatives.Should().Be(60);
        _matrix.FalsePositives.Should().Be(10);
        _matrix.FalseNegatives.Should().Be(40);
    }

    [Fact]
    public void Observations_SumsAllCells()
    {
        _matrix.Observations.Should().Be(200);
    }

    [Fact]
    public void ActualPositives_IsTpPlusFn()
    {
        _matrix.ActualPositives.Should().Be(130); // 90 + 40
    }

    [Fact]
    public void ActualNegatives_IsTnPlusFp()
    {
        _matrix.ActualNegatives.Should().Be(70); // 60 + 10
    }

    [Fact]
    public void Accuracy_IsCorrect()
    {
        // (90 + 60) / 200 = 0.75
        _matrix.Accuracy.Should().BeApproximately(0.75, 1e-10);
    }

    [Fact]
    public void Sensitivity_IsCorrect()
    {
        // TPR = 90 / (90 + 40) = 90/130 ≈ 0.6923
        _matrix.Sensitivity.Should().BeApproximately(90.0 / 130.0, 1e-10);
    }

    [Fact]
    public void Specificity_IsCorrect()
    {
        // TNR = 60 / (60 + 10) = 60/70 ≈ 0.8571
        _matrix.Specificity.Should().BeApproximately(60.0 / 70.0, 1e-10);
    }

    [Fact]
    public void PositivePredictiveValue_IsCorrect()
    {
        // PPV = 90 / (90 + 10) = 0.9
        _matrix.PositivePredictiveValue.Should().BeApproximately(0.9, 1e-10);
    }

    [Fact]
    public void NegativePredictiveValue_IsCorrect()
    {
        // NPV = 60 / (60 + 40) = 0.6
        _matrix.NegativePredictiveValue.Should().BeApproximately(0.6, 1e-10);
    }

    [Fact]
    public void FalseDiscoveryRate_IsCorrect()
    {
        // FDR = 10 / (10 + 90) = 0.1
        _matrix.FalseDiscoveryRate.Should().BeApproximately(0.1, 1e-10);
    }

    [Fact]
    public void FalsePositiveRate_IsCorrect()
    {
        // FPR = 10 / (10 + 60) = 10/70 ≈ 0.1429
        _matrix.FalsePositiveRate.Should().BeApproximately(10.0 / 70.0, 1e-10);
    }

    [Fact]
    public void Efficiency_IsAverageOfSensitivityAndSpecificity()
    {
        var expected = (_matrix.Sensitivity + _matrix.Specificity) / 2;
        _matrix.Efficiency.Should().BeApproximately(expected, 1e-10);
    }

    [Fact]
    public void PerfectClassifier_HasMccOfOne()
    {
        var perfect = new ConfusionMatrix(50, 50, 0, 0);
        perfect.MatthewsCorrelationCoefficient.Should().BeApproximately(1.0, 1e-10);
    }

    [Fact]
    public void BooleanConstructor_ComputesCorrectly()
    {
        bool[] predicted = [true, true, false, false, true];
        bool[] expected  = [true, false, false, true, true];
        var cm = new ConfusionMatrix(predicted, expected);

        cm.TruePositives.Should().Be(2);
        cm.TrueNegatives.Should().Be(1);
        cm.FalsePositives.Should().Be(1);
        cm.FalseNegatives.Should().Be(1);
    }

    [Fact]
    public void NoPredictedPositives_PpvReturnsOne()
    {
        var cm = new ConfusionMatrix(0, 50, 0, 50);
        cm.PositivePredictiveValue.Should().Be(1.0);
    }
}
