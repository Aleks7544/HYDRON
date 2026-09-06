using HYDRON.Core;

namespace HYDRON.Tests;

public class SystemConstantsTests
{
    [Theory]
    [InlineData(0, 0, false)]   // zero total
    [InlineData(0, 1, false)]   // zero approvals
    [InlineData(1, 1, true)]    // unanimous single
    [InlineData(2, 3, true)]    // exact 2/3
    [InlineData(67, 100, true)] // just above 2/3
    [InlineData(66, 100, false)]// just below ceiling (ceil(100*2/3)=67)
    [InlineData(65, 100, false)]// clearly below
    [InlineData(3, 3, true)]    // unanimous triple
    [InlineData(1, 3, false)]   // one of three
    public void IsSupermajority_ReturnsExpected(int approvals, int total, bool expected)
        => Assert.Equal(expected, SystemConstants.IsSupermajority(approvals, total));

    [Fact]
    public void SupermajorityThreshold_IsCorrect()
        => Assert.Equal(2.0 / 3.0, SystemConstants.SupermajorityThreshold);

    [Fact]
    public void PhysicsConstant_IsCorrect()
        => Assert.Equal(13.6m, SystemConstants.HydrogenIonizationEnergyEv);

    [Fact]
    public void BlockCapacities_AreCorrect()
    {
        Assert.Equal(100, SystemConstants.TransactionsPerBlock);
        Assert.Equal(100, SystemConstants.BlocksPerStateBlock);
        Assert.Equal(100, SystemConstants.ImmutabilityDepth);
    }

    [Fact]
    public void RewardAmounts_MatchDenominations()
    {
        // 1 HYA = 100 atomos
        Assert.Equal(new HYDRON.Models.Atomos(100), SystemConstants.TxReward);
        // 1 HYB = 10_000 atomos
        Assert.Equal(new HYDRON.Models.Atomos(10_000), SystemConstants.TransactionBlockReward);
        // 1 HYG = 100_000_000 atomos
        Assert.Equal(new HYDRON.Models.Atomos(100_000_000), SystemConstants.StateBlockReward);
        // 1 HYD = 10^16 atomos
        Assert.Equal(new HYDRON.Models.Atomos(new System.Numerics.BigInteger(10_000_000_000_000_000L)), SystemConstants.MinimumFee);
    }
}
