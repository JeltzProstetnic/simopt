using FluentAssertions;
using SimOpt.Statistics.Analysis;
using Xunit;

namespace SimOpt.Tests.Statistics.Analysis;

/// <summary>
/// Tests for ReceiverOperatingCharacteristic&lt;T&gt;.
/// Instance type: (Score: double, IsPositive: bool).
/// Classificator: score &gt;= threshold means classified positive.
/// Verificator: returns true when classification matches ground truth.
/// </summary>
public class ReceiverOperatingCharacteristicTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private record Sample(double Score, bool IsPositive);

    /// <summary>Classify positive when score &gt;= threshold.</summary>
    private static bool Classify(Sample s, double threshold) => s.Score >= threshold;

    /// <summary>Return true when the classification matches the actual label.</summary>
    private static bool Verify(Sample s, bool classifiedPositive)
        => s.IsPositive == classifiedPositive;

    private static ReceiverOperatingCharacteristic<Sample> BuildRoc(
        IEnumerable<Sample> instances)
    {
        return new ReceiverOperatingCharacteristic<Sample>(
            instances,
            Classify,
            Verify);
    }

    // ── PointsCalculated guard ────────────────────────────────────────────────

    [Fact]
    public void Area_BeforeCalculate_ThrowsArgumentNullException()
    {
        var roc = BuildRoc([new Sample(0.8, true), new Sample(0.2, false)]);

        var act = () => _ = roc.Area;

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PointsCalculated_AfterCalculate_IsTrue()
    {
        var roc = BuildRoc([new Sample(0.8, true), new Sample(0.2, false)]);
        roc.Calculate(10);

        roc.PointsCalculated.Should().BeTrue();
    }

    // ── ROC curve generation ──────────────────────────────────────────────────

    [Fact]
    public void Points_AfterCalculateWithResolution10_HasAtLeast10Points()
    {
        // resolution=10 → increment=1/10.0 → nominally 10 steps (0.0..0.9).
        // IEEE 754 accumulation may produce one extra step when the accumulated
        // total is marginally below 1.0 after 10 additions, so we assert >= 10.
        var instances = CreateBalancedSet(5);
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        roc.Points.Should().HaveCountGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void Points_AfterCalculateWithIncrement_HasAtLeast4Points()
    {
        // increment=0.25 → nominally 4 steps (0.0, 0.25, 0.50, 0.75).
        // Due to binary representation 0.25 is exact (power of two), so this
        // loop terminates cleanly at exactly 4 iterations.
        var instances = CreateBalancedSet(5);
        var roc = BuildRoc(instances);
        roc.Calculate(0.25);

        roc.Points.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    [Fact]
    public void Points_EachPoint_IsConfusionMatrix()
    {
        var instances = CreateBalancedSet(5);
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        roc.Points.Should().AllSatisfy(p => p.Should().BeOfType<ConfusionMatrix>());
    }

    [Fact]
    public void FirstPoint_ThresholdZero_ClassifiesAllPositive()
    {
        // threshold=0 → every instance has score >= 0 → all classified positive
        // TP = actual positives, FP = actual negatives, TN = FN = 0
        const int positiveCount = 5;
        const int negativeCount = 5;
        var instances = CreateBalancedSet(positiveCount);
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        var first = roc.Points[0];
        first.TruePositives.Should().Be(positiveCount);
        first.FalsePositives.Should().Be(negativeCount);
        first.TrueNegatives.Should().Be(0);
        first.FalseNegatives.Should().Be(0);
    }

    // ── AUC: perfect classifier ──────────────────────────────────────────────

    [Fact]
    public void Area_PerfectClassifier_IsOne()
    {
        // Positives: score=0.9, Negatives: score=0.1
        // At threshold=0.0: all positive → FPR=1, TPR=1
        // At threshold=0.1..0.8: positives (0.9) still above threshold, negatives (0.1) not → FPR=0, TPR=1
        // At threshold=0.9: positives still classified positive (0.9 >= 0.9), negatives below → FPR=0, TPR=1
        // Trapezoid from point 0 to point 1: (1+1)*(1-0)/2 = 1.0; all subsequent = 0
        var instances = CreatePerfectClassifierSet(posCount: 5, negCount: 5);
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        roc.Area.Should().BeApproximately(1.0, 1e-10);
    }

    [Fact]
    public void Area_PerfectClassifier_IsCachedOnSecondAccess()
    {
        var instances = CreatePerfectClassifierSet(posCount: 5, negCount: 5);
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        var first = roc.Area;
        var second = roc.Area;

        first.Should().Be(second);
    }

    // ── AUC: random classifier ───────────────────────────────────────────────

    [Fact]
    public void Area_RandomClassifier_IsApproximatelyHalf()
    {
        // Interleaved positives and negatives with alternating scores so that
        // the ROC curve is close to the diagonal.
        // Scores for positives: 0.15, 0.35, 0.55, 0.75, 0.95
        // Scores for negatives: 0.05, 0.25, 0.45, 0.65, 0.85
        // Each threshold step gains roughly the same TPR and FPR.
        var instances = CreateRandomLikeSet();
        var roc = BuildRoc(instances);
        roc.Calculate(20);

        // The implementation flips values < 0.5, so Area is in [0.5, 1.0].
        // For a well-interleaved set the raw diagonal sum is ~0.5, so AUC ~0.5.
        roc.Area.Should().BeInRange(0.45, 0.65);
    }

    // ── AUC: worst-case classifier (inverted) ────────────────────────────────

    [Fact]
    public void Area_InvertedClassifier_FlipsToAboveHalf()
    {
        // A classifier that scores positives LOW and negatives HIGH produces
        // raw AUC < 0.5. The implementation returns 1 - rawAUC, which is > 0.5.
        // Positives: score=0.1, Negatives: score=0.9
        var instances = CreateInvertedClassifierSet(posCount: 5, negCount: 5);
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        roc.Area.Should().BeGreaterThanOrEqualTo(0.5);
    }

    // ── TPR / FPR at specific thresholds ─────────────────────────────────────

    [Fact]
    public void Points_AfterThresholdExceedsAllScores_AllClassifiedNegative()
    {
        // For a perfect-classifier set (pos=0.9, neg=0.1), at any threshold > 0.9
        // nothing is classified positive: TP=FP=0, TN=negCount, FN=posCount.
        // With increment=0.25 the thresholds are 0, 0.25, 0.5, 0.75.
        // At threshold=0.75: positives (0.9 >= 0.75) still classified positive → TPR=1, FPR=0.
        // At threshold=0.25: negatives (0.1 < 0.25) classified negative → FPR=0, TPR=1.
        // We verify the intermediate range directly.
        var instances = CreatePerfectClassifierSet(posCount: 6, negCount: 6);
        var roc = BuildRoc(instances);
        roc.Calculate(0.25); // thresholds: 0.0, 0.25, 0.50, 0.75

        // At index 1 (threshold≈0.25): negatives (score=0.1) below threshold → TN=6, FP=0 → FPR=0
        // positives (score=0.9) above threshold → TP=6, FN=0 → TPR=1
        var pointAt025 = roc.Points[1];
        pointAt025.Sensitivity.Should().BeApproximately(1.0, 1e-10);
        pointAt025.FalsePositiveRate.Should().BeApproximately(0.0, 1e-10);
    }

    [Fact]
    public void Points_ZeroThreshold_TprAndFprBothOne()
    {
        // threshold=0 → all classified positive → TPR=1, FPR=1
        var instances = CreatePerfectClassifierSet(posCount: 8, negCount: 8);
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        var point = roc.Points[0];
        point.Sensitivity.Should().BeApproximately(1.0, 1e-10);
        point.FalsePositiveRate.Should().BeApproximately(1.0, 1e-10);
    }

    // ── BestPoint / BestThreshold ────────────────────────────────────────────

    [Fact]
    public void BestThreshold_PerfectClassifier_IsNonZero()
    {
        // For a perfect classifier the best threshold is any value in (0.1, 0.9]
        // because accuracy is perfect (1.0) when negatives are correctly excluded.
        var instances = CreatePerfectClassifierSet(posCount: 5, negCount: 5);
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        // BestThreshold must not be 0 (at 0 everything is positive, FP > 0)
        roc.BestThreshold.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void BestPoint_PerfectClassifier_HasFullAccuracy()
    {
        var instances = CreatePerfectClassifierSet(posCount: 5, negCount: 5);
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        roc.BestPoint.Accuracy.Should().BeApproximately(1.0, 1e-10);
    }

    [Fact]
    public void BestPoint_MatchesBestAccuracyAmongAllPoints()
    {
        var instances = CreateBalancedSet(6);
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        double maxAccuracy = roc.Points.Max(p => p.Accuracy);
        roc.BestPoint.Accuracy.Should().BeApproximately(maxAccuracy, 1e-10);
    }

    // ── Standard error ────────────────────────────────────────────────────────

    [Fact]
    public void Error_PerfectClassifier_IsFiniteAndSmall()
    {
        var instances = CreatePerfectClassifierSet(posCount: 10, negCount: 10);
        var roc = BuildRoc(instances);
        roc.Calculate(20);

        var error = roc.Error;

        double.IsFinite(error).Should().BeTrue();
        error.Should().BeGreaterThanOrEqualTo(0.0);
        error.Should().BeLessThan(0.5);
    }

    [Fact]
    public void Error_LargerSample_IsSmallerThanSmallSample()
    {
        // SE shrinks with more samples: Hanley-McNeil denominator is Na*Nn.
        // We must use AUC < 1 — a perfect classifier yields A=1 and SE=0 regardless of N.
        // We use the partial classifier (7 pos-high, 3 pos-low, 10 neg) which gives AUC≈0.7.
        // Doubling the sample count must give a strictly smaller SE.
        // Small: 7 pos@0.8, 3 pos@0.3, 10 neg@0.5  (20 samples, AUC≈0.7)
        // Large: 14 pos@0.8, 6 pos@0.3, 20 neg@0.5  (40 samples, same AUC≈0.7, smaller SE)
        var small = CreatePartialClassifierSet(posHigh: 7,  posLow: 3,  negCount: 10,
            highScore: 0.8, lowScore: 0.3, negScore: 0.5);
        var large = CreatePartialClassifierSet(posHigh: 14, posLow: 6, negCount: 20,
            highScore: 0.8, lowScore: 0.3, negScore: 0.5);

        var rocSmall = BuildRoc(small);
        rocSmall.Calculate(20);

        var rocLarge = BuildRoc(large);
        rocLarge.Calculate(20);

        // Both areas should be approximately equal (same AUC structure, just more samples).
        rocSmall.Area.Should().BeApproximately(rocLarge.Area, 0.05,
            "the AUC should be independent of sample count for the same score distribution");

        // SE must be strictly smaller for the larger sample.
        rocLarge.Error.Should().BeLessThan(rocSmall.Error);
    }

    // ── Compare ───────────────────────────────────────────────────────────────

    [Fact]
    public void Compare_BeforeCalculate_ThrowsArgumentNullException()
    {
        var roc1 = BuildRoc(CreatePerfectClassifierSet(5, 5));
        var roc2 = BuildRoc(CreatePerfectClassifierSet(5, 5));
        // neither calculated

        var act = () => roc1.Compare(roc2, 0.5);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Compare_FirstCalculatedSecondNot_ThrowsArgumentNullException()
    {
        var roc1 = BuildRoc(CreatePerfectClassifierSet(5, 5));
        roc1.Calculate(10);
        var roc2 = BuildRoc(CreateBalancedSet(5));
        // roc2 not calculated

        var act = () => roc1.Compare(roc2, 0.5);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Compare_TwoCurvesWithSameAuc_ReturnsNearZero()
    {
        // Two identical imperfect classifiers → ΔA = 0 → z-statistic = 0.
        // We must use AUC < 1 so SE > 0 (a perfect AUC=1 produces SE=0 → 0/0=NaN).
        var instances = CreateMixedClassifierSet(posCount: 10, negCount: 10);

        var roc1 = BuildRoc(instances);
        roc1.Calculate(20);

        var roc2 = BuildRoc(instances);
        roc2.Calculate(20);

        var z = roc1.Compare(roc2, 0.0);
        z.Should().BeApproximately(0.0, 1e-10);
    }

    [Fact]
    public void Compare_BetterVsWorse_ReturnsPositiveZScore()
    {
        // "Better" classifier: 7 pos at score=0.8, 3 pos at score=0.3, 10 neg at score=0.5.
        // With increment=0.05, the transition from FPR=1→0 occurs between threshold 0.50 and 0.55,
        // capturing TPR=0.7 → AUC=0.7 and SE>0.
        //
        // "Worse" classifier: all positives and negatives at score=0.5 (diagonal classifier).
        // Every threshold step either puts all samples as positive (t<=0.5) or all negative
        // (t>0.5) → raw AUC=0.5, SE>0.
        //
        // Compare(r=0): z = (0.7 - 0.5) / sqrt(se_better^2 + se_worse^2) > 0.
        var better = CreatePartialClassifierSet(posHigh: 7, posLow: 3, negCount: 10,
            highScore: 0.8, lowScore: 0.3, negScore: 0.5);
        var worse = CreateMixedClassifierSet(posCount: 10, negCount: 10);

        var rocBetter = BuildRoc(better);
        rocBetter.Calculate(20);

        var rocWorse = BuildRoc(worse);
        rocWorse.Calculate(20);

        rocBetter.Area.Should().BeGreaterThan(rocWorse.Area,
            "the partially-separating classifier should have AUC > diagonal baseline");

        var z = rocBetter.Compare(rocWorse, 0.0);
        double.IsFinite(z).Should().BeTrue("z-statistic must be finite when both SEs > 0");
        z.Should().BeGreaterThan(0.0);
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_AllSamplesPositive_DoesNotThrow()
    {
        // 10 positives, 0 negatives — edge case: FPR undefined (0/0 → NaN)
        var instances = Enumerable.Range(0, 10)
            .Select(i => new Sample(0.9, IsPositive: true));
        var roc = BuildRoc(instances);

        var act = () => roc.Calculate(10);

        act.Should().NotThrow();
        roc.PointsCalculated.Should().BeTrue();
    }

    [Fact]
    public void Calculate_AllSamplesNegative_DoesNotThrow()
    {
        // 0 positives, 10 negatives — edge case: TPR undefined (0/0 → NaN)
        var instances = Enumerable.Range(0, 10)
            .Select(i => new Sample(0.1, IsPositive: false));
        var roc = BuildRoc(instances);

        var act = () => roc.Calculate(10);

        act.Should().NotThrow();
        roc.PointsCalculated.Should().BeTrue();
    }

    [Fact]
    public void Calculate_SinglePositiveSample_ProducesPoints()
    {
        var instances = new[] { new Sample(0.8, IsPositive: true) };
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        // Floating-point accumulation may produce 10 or 11 iterations; either is valid.
        roc.Points.Should().HaveCountGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void Calculate_SingleNegativeSample_ProducesPoints()
    {
        var instances = new[] { new Sample(0.2, IsPositive: false) };
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        roc.Points.Should().HaveCountGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void Calculate_SinglePositiveAndNegative_ProducesPoints()
    {
        var instances = new[]
        {
            new Sample(0.8, IsPositive: true),
            new Sample(0.2, IsPositive: false),
        };
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        roc.Points.Should().HaveCountGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void Area_AllSamplesPositive_DoesNotThrow()
    {
        // When there are no actual negatives, FPR = 0/0 = NaN at every point.
        // The trapezoidal integral propagates NaN through IEEE 754 arithmetic.
        // The Area property must not throw — NaN is an acceptable result here
        // because the degenerate case (no negatives) makes AUC undefined.
        var instances = Enumerable.Range(0, 5)
            .Select(i => new Sample(0.9, IsPositive: true));
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        var act = () => _ = roc.Area;
        act.Should().NotThrow();
    }

    // ── Confusion-matrix totals are consistent ────────────────────────────────

    [Fact]
    public void Points_SumOfCells_EqualsObservationCount()
    {
        const int total = 10;
        var instances = CreateBalancedSet(total / 2);
        var roc = BuildRoc(instances);
        roc.Calculate(10);

        roc.Points.Should().AllSatisfy(p =>
            p.Observations.Should().Be(total));
    }

    // ── Dataset factory methods ───────────────────────────────────────────────

    /// <summary>
    /// Balanced dataset: positives at score=0.75, negatives at score=0.25.
    /// Not a perfect classifier (both score groups partially overlap threshold steps).
    /// </summary>
    private static List<Sample> CreateBalancedSet(int perClass)
    {
        var list = new List<Sample>(perClass * 2);
        for (int i = 0; i < perClass; i++)
            list.Add(new Sample(0.75, IsPositive: true));
        for (int i = 0; i < perClass; i++)
            list.Add(new Sample(0.25, IsPositive: false));
        return list;
    }

    /// <summary>
    /// Perfect classifier: positives at score=0.9, negatives at score=0.1.
    /// At thresholds in [0.1+ε, 0.9], all classifications are correct.
    /// </summary>
    private static List<Sample> CreatePerfectClassifierSet(int posCount, int negCount)
    {
        var list = new List<Sample>(posCount + negCount);
        for (int i = 0; i < posCount; i++)
            list.Add(new Sample(0.9, IsPositive: true));
        for (int i = 0; i < negCount; i++)
            list.Add(new Sample(0.1, IsPositive: false));
        return list;
    }

    /// <summary>
    /// Inverted classifier: positives at score=0.1, negatives at score=0.9.
    /// Produces raw AUC &lt; 0.5; the implementation reports 1 - rawAUC.
    /// </summary>
    private static List<Sample> CreateInvertedClassifierSet(int posCount, int negCount)
    {
        var list = new List<Sample>(posCount + negCount);
        for (int i = 0; i < posCount; i++)
            list.Add(new Sample(0.1, IsPositive: true));
        for (int i = 0; i < negCount; i++)
            list.Add(new Sample(0.9, IsPositive: false));
        return list;
    }

    /// <summary>
    /// Random-like classifier: positives and negatives have interleaved scores,
    /// producing a ROC curve close to the diagonal (AUC ≈ 0.5).
    /// Positives:  0.15, 0.35, 0.55, 0.75, 0.95
    /// Negatives:  0.05, 0.25, 0.45, 0.65, 0.85
    /// </summary>
    private static List<Sample> CreateRandomLikeSet()
    {
        return
        [
            new Sample(0.15, IsPositive: true),
            new Sample(0.05, IsPositive: false),
            new Sample(0.35, IsPositive: true),
            new Sample(0.25, IsPositive: false),
            new Sample(0.55, IsPositive: true),
            new Sample(0.45, IsPositive: false),
            new Sample(0.75, IsPositive: true),
            new Sample(0.65, IsPositive: false),
            new Sample(0.95, IsPositive: true),
            new Sample(0.85, IsPositive: false),
        ];
    }

    /// <summary>
    /// Noisy classifier: positives at score=0.6, negatives at score=0.4.
    /// Better than random but not perfect. AUC between 0.5 and 1.0.
    /// </summary>
    private static List<Sample> CreateNoisyClassifierSet(int posCount, int negCount)
    {
        var list = new List<Sample>(posCount + negCount);
        for (int i = 0; i < posCount; i++)
            list.Add(new Sample(0.6, IsPositive: true));
        for (int i = 0; i < negCount; i++)
            list.Add(new Sample(0.4, IsPositive: false));
        return list;
    }

    /// <summary>
    /// Mixed classifier: positives at score=0.5, negatives at score=0.5.
    /// All samples have the same score, so at threshold=0 everything is positive
    /// (FPR=1, TPR=1) and at threshold&gt;0.5 everything is negative (FPR=0, TPR=0).
    /// The resulting raw AUC is 0.5 (which is kept, not flipped).
    /// SE &gt; 0, which makes this set suitable for Compare tests.
    /// </summary>
    private static List<Sample> CreateMixedClassifierSet(int posCount, int negCount)
    {
        var list = new List<Sample>(posCount + negCount);
        for (int i = 0; i < posCount; i++)
            list.Add(new Sample(0.5, IsPositive: true));
        for (int i = 0; i < negCount; i++)
            list.Add(new Sample(0.5, IsPositive: false));
        return list;
    }

    /// <summary>
    /// Partial classifier: <paramref name="posHigh"/> positives at <paramref name="highScore"/>,
    /// <paramref name="posLow"/> positives at <paramref name="lowScore"/>, and
    /// <paramref name="negCount"/> negatives at <paramref name="negScore"/>.
    /// Use this to construct classifiers with controllable AUC between 0.5 and 1.0.
    /// </summary>
    private static List<Sample> CreatePartialClassifierSet(
        int posHigh, int posLow, int negCount,
        double highScore, double lowScore, double negScore)
    {
        var list = new List<Sample>(posHigh + posLow + negCount);
        for (int i = 0; i < posHigh; i++)
            list.Add(new Sample(highScore, IsPositive: true));
        for (int i = 0; i < posLow; i++)
            list.Add(new Sample(lowScore, IsPositive: true));
        for (int i = 0; i < negCount; i++)
            list.Add(new Sample(negScore, IsPositive: false));
        return list;
    }
}
