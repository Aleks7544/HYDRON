using HYDRON.Models;
using HYDRON.Validator;

namespace HYDRON.Tests;

public class ValidatorSelectorTests
{
    private static ValidatorRank MakeRank(
        string address,
        ValidatorTier tier,
        double finalRank) =>
        new()
        {
            ValidatorAddress = address,
            StakedAmount = new Atomos(1000),
            AvgValidationSpeedMs = 100.0,
            ValidationActivityCount = 10,
            ReputationNormalized = 0.8,
            BlocksObserved = 50,
            ComputedAt = DateTimeOffset.UtcNow,
            FinalRank = finalRank,
            Tier = tier
        };

    // --- Guards ---

    [Fact]
    public void Select_NullRanked_Throws()
        => Assert.Throws<ArgumentNullException>(() => ValidatorSelector.Select(null!, 1));

    [Fact]
    public void Select_ZeroCount_Throws()
        => Assert.Throws<ArgumentException>(() =>
            ValidatorSelector.Select([MakeRank("v1", ValidatorTier.Core, 80.0)], 0));

    [Fact]
    public void Select_NegativeCount_Throws()
        => Assert.Throws<ArgumentException>(() =>
            ValidatorSelector.Select([MakeRank("v1", ValidatorTier.Core, 80.0)], -1));

    [Fact]
    public void Select_EmptyRanked_Throws()
        => Assert.Throws<ArgumentException>(() => ValidatorSelector.Select([], 1));

    [Fact]
    public void Select_CountExceedsAvailable_Throws()
        => Assert.Throws<ArgumentException>(() =>
            ValidatorSelector.Select([MakeRank("v1", ValidatorTier.Core, 80.0)], 2));

    // --- Core-first ordering ---

    [Fact]
    public void Select_CoreBeforeEdge_RegardlessOfFinalRank()
    {
        var ranked = new List<ValidatorRank>
        {
            MakeRank("edge-high", ValidatorTier.Edge, 99.0),
            MakeRank("core-low",  ValidatorTier.Core, 10.0),
        };

        var result = ValidatorSelector.Select(ranked, 1);
        Assert.Equal("core-low", result[0].ValidatorAddress);
    }

    [Fact]
    public void Select_WithinCoreTier_OrderedByFinalRankDescending()
    {
        var ranked = new List<ValidatorRank>
        {
            MakeRank("c1", ValidatorTier.Core, 60.0),
            MakeRank("c2", ValidatorTier.Core, 90.0),
            MakeRank("c3", ValidatorTier.Core, 75.0),
        };

        var result = ValidatorSelector.Select(ranked, 3);
        Assert.Equal("c2", result[0].ValidatorAddress);
        Assert.Equal("c3", result[1].ValidatorAddress);
        Assert.Equal("c1", result[2].ValidatorAddress);
    }

    [Fact]
    public void Select_WithinEdgeTier_OrderedByFinalRankDescending()
    {
        var ranked = new List<ValidatorRank>
        {
            MakeRank("e1", ValidatorTier.Edge, 40.0),
            MakeRank("e2", ValidatorTier.Edge, 70.0),
        };

        var result = ValidatorSelector.Select(ranked, 2);
        Assert.Equal("e2", result[0].ValidatorAddress);
        Assert.Equal("e1", result[1].ValidatorAddress);
    }

    [Fact]
    public void Select_MixedTiers_CoreFirstThenEdgeByRank()
    {
        var ranked = new List<ValidatorRank>
        {
            MakeRank("e1", ValidatorTier.Edge, 95.0),
            MakeRank("c1", ValidatorTier.Core, 50.0),
            MakeRank("c2", ValidatorTier.Core, 80.0),
            MakeRank("e2", ValidatorTier.Edge, 30.0),
        };

        var result = ValidatorSelector.Select(ranked, 4);
        Assert.Equal("c2", result[0].ValidatorAddress);
        Assert.Equal("c1", result[1].ValidatorAddress);
        Assert.Equal("e1", result[2].ValidatorAddress);
        Assert.Equal("e2", result[3].ValidatorAddress);
    }

    // --- Exact count ---

    [Fact]
    public void Select_ExactCount_ReturnedCorrectly()
    {
        var ranked = new List<ValidatorRank>
        {
            MakeRank("v1", ValidatorTier.Core, 90.0),
            MakeRank("v2", ValidatorTier.Core, 80.0),
            MakeRank("v3", ValidatorTier.Edge, 70.0),
        };

        var result = ValidatorSelector.Select(ranked, 2);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Select_Index0_IsHighestRankedCore()
    {
        var ranked = new List<ValidatorRank>
        {
            MakeRank("c-low",  ValidatorTier.Core, 40.0),
            MakeRank("c-high", ValidatorTier.Core, 90.0),
            MakeRank("e1",     ValidatorTier.Edge, 99.0),
        };

        var result = ValidatorSelector.Select(ranked, 3);
        Assert.Equal("c-high", result[0].ValidatorAddress);
    }
}
