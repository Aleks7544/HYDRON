using System.Numerics;
using HYDRON.Models;

namespace HYDRON.Tests;

public class RewardsTests
{
    private static ValidatorReward MakeValidatorReward(
        string address = "validator1",
        ValidatorTier tier = ValidatorTier.Core,
        int txsValidated = 10,
        int blockAmt = 100,
        int validationAmt = 50,
        int feeAmt = 25) =>
        new(address, tier, txsValidated,
            new Atomos(blockAmt), new Atomos(validationAmt), new Atomos(feeAmt));

    private static BlockReward MakeBlockReward(
        IEnumerable<ValidatorReward>? rewards = null) =>
        new(
            blockNumber: BigInteger.One,
            coreCapacityAtBlock: 10,
            totalValidatorsAtBlock: 20,
            averageValidationTimeMs: 150.0,
            totalCoreBlockReward: new Atomos(1000),
            totalEdgeBlockReward: new Atomos(500),
            totalValidationReward: new Atomos(200),
            totalFeeReward: new Atomos(100),
            validatorRewards: rewards ?? [MakeValidatorReward()]);

    // --- ValidatorReward construction ---

    [Fact]
    public void ValidatorReward_ValidArgs_ComputesTotalCorrectly()
    {
        var vr = MakeValidatorReward(blockAmt: 100, validationAmt: 50, feeAmt: 25);
        Assert.Equal(new Atomos(175), vr.TotalReward);
    }

    [Fact]
    public void ValidatorReward_EmptyAddress_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new ValidatorReward("", ValidatorTier.Core, 10,
                new Atomos(100), new Atomos(50), new Atomos(25)));

    [Fact]
    public void ValidatorReward_NegativeTxsValidated_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new ValidatorReward("addr", ValidatorTier.Core, -1,
                new Atomos(100), new Atomos(50), new Atomos(25)));

    [Fact]
    public void ValidatorReward_EdgeTier_StoredCorrectly()
    {
        var vr = new ValidatorReward("addr", ValidatorTier.Edge, 5,
            new Atomos(10), new Atomos(5), new Atomos(2));
        Assert.Equal(ValidatorTier.Edge, vr.Tier);
    }

    [Fact]
    public void ValidatorReward_ZeroAmounts_TotalIsZero()
    {
        var vr = new ValidatorReward("addr", ValidatorTier.Core, 0,
            Atomos.Zero, Atomos.Zero, Atomos.Zero);
        Assert.Equal(Atomos.Zero, vr.TotalReward);
    }

    // --- BlockReward construction ---

    [Fact]
    public void BlockReward_ValidArgs_ComputesTotalMintedCorrectly()
    {
        // TotalMinted = core + edge + validation (fees excluded)
        var br = MakeBlockReward();
        Assert.Equal(new Atomos(1700), br.TotalMinted);
    }

    [Fact]
    public void BlockReward_NegativeBlockNumber_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new BlockReward(new BigInteger(-1), 10, 20, 100.0,
                new Atomos(1000), new Atomos(500), new Atomos(200), new Atomos(100),
                [MakeValidatorReward()]));

    [Fact]
    public void BlockReward_NegativeCoreCapacity_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new BlockReward(BigInteger.One, -1, 20, 100.0,
                new Atomos(1000), new Atomos(500), new Atomos(200), new Atomos(100),
                [MakeValidatorReward()]));

    [Fact]
    public void BlockReward_NegativeTotalValidators_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new BlockReward(BigInteger.One, 10, -1, 100.0,
                new Atomos(1000), new Atomos(500), new Atomos(200), new Atomos(100),
                [MakeValidatorReward()]));

    [Fact]
    public void BlockReward_NegativeAvgValidationTime_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new BlockReward(BigInteger.One, 10, 20, -1.0,
                new Atomos(1000), new Atomos(500), new Atomos(200), new Atomos(100),
                [MakeValidatorReward()]));

    [Fact]
    public void BlockReward_NullValidatorRewards_Throws()
        => Assert.Throws<ArgumentNullException>(() =>
            new BlockReward(BigInteger.One, 10, 20, 100.0,
                new Atomos(1000), new Atomos(500), new Atomos(200), new Atomos(100),
                null!));

    [Fact]
    public void BlockReward_EmptyValidatorRewards_IsAllowed()
    {
        var br = new BlockReward(BigInteger.One, 10, 20, 100.0,
            new Atomos(1000), new Atomos(500), new Atomos(200), new Atomos(100), []);
        Assert.Empty(br.ValidatorRewards);
    }

    [Fact]
    public void BlockReward_InitialStatus_IsPending()
        => Assert.Equal(RewardStatus.Pending, MakeBlockReward().Status);

    [Fact]
    public void BlockReward_HasUniqueId()
    {
        var br1 = MakeBlockReward();
        var br2 = MakeBlockReward();
        Assert.NotEqual(br1.Id, br2.Id);
    }

    [Fact]
    public void BlockReward_ValidatorRewards_AreStored()
    {
        var vr1 = MakeValidatorReward("v1");
        var vr2 = MakeValidatorReward("v2");
        var br = MakeBlockReward([vr1, vr2]);
        Assert.Equal(2, br.ValidatorRewards.Count);
    }

    // --- Settle ---

    [Fact]
    public void Settle_SetsStatusToSettled()
    {
        var br = MakeBlockReward();
        br.Settle();
        Assert.Equal(RewardStatus.Settled, br.Status);
        Assert.NotNull(br.SettledAt);
    }

    [Fact]
    public void Settle_Twice_Throws()
    {
        var br = MakeBlockReward();
        br.Settle();
        Assert.Throws<InvalidOperationException>(() => br.Settle());
    }

    [Fact]
    public void Settle_SettledAt_IsAfterIssuedAt()
    {
        var br = MakeBlockReward();
        br.Settle();
        Assert.True(br.SettledAt >= br.IssuedAt);
    }
}
