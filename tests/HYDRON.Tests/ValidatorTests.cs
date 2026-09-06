using System.Numerics;
using HYDRON.Models;
using Xunit;

namespace HYDRON.Tests;

public class ValidatorTests
{
    private static Validator MakeValidator(
        string address = "val_addr",
        Atomos? stake = null,
        string? ipv4 = "192.168.1.1") =>
        new(address, "pubkey", "stealth", stake ?? new Atomos(1000), networkEndpointIPv4: ipv4);

    [Fact]
    public void Constructor_ValidArgs_InitialisesCorrectly()
    {
        Validator v = MakeValidator();
        Assert.Equal(new Atomos(1000), v.StakedAmount);
        Assert.Equal(ValidatorStatus.Active, v.Status);
        Assert.Equal(ValidatorTier.Edge, v.Tier);
        Assert.Equal("192.168.1.1", v.NetworkEndpointIPv4);
    }

    [Fact]
    public void Constructor_ZeroStake_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new Validator("addr", "pub", "stealth", Atomos.Zero, networkEndpointIPv4: "1.2.3.4"));

    [Fact]
    public void Constructor_NoEndpoint_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new Validator("addr", "pub", "stealth", new Atomos(1)));

    [Fact]
    public void Constructor_InvalidIPv4_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new Validator("addr", "pub", "stealth", new Atomos(1), networkEndpointIPv4: "not_an_ip"));

    [Fact]
    public void Constructor_InvalidIPv6_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new Validator("addr", "pub", "stealth", new Atomos(1), networkEndpointIPv6: "not_an_ipv6"));

    [Fact]
    public void Constructor_InvalidDns_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new Validator("addr", "pub", "stealth", new Atomos(1), networkEndpointDns: "-invalid-.com"));

    [Fact]
    public void Constructor_InvalidCommissionRate_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new Validator("addr", "pub", "stealth", new Atomos(1), networkEndpointIPv4: "1.2.3.4", commissionRate: 101.0));

    [Fact]
    public void AddStake_IncreasesStakedAmount()
    {
        Validator v = MakeValidator();
        v.AddStake(new Atomos(500));
        Assert.Equal(new Atomos(1500), v.StakedAmount);
    }

    [Fact]
    public void AddStake_ZeroAmount_Throws()
        => Assert.Throws<ArgumentException>(() => MakeValidator().AddStake(Atomos.Zero));

    [Fact]
    public void WithdrawStake_ValidAmount_DecreasesStake()
    {
        Validator v = MakeValidator(stake: new Atomos(1000));
        v.WithdrawStake(new Atomos(400));
        Assert.Equal(new Atomos(600), v.StakedAmount);
    }

    [Fact]
    public void WithdrawStake_ExceedsStake_Throws()
        => Assert.Throws<InvalidOperationException>(() =>
            MakeValidator(stake: new Atomos(100)).WithdrawStake(new Atomos(200)));

    [Fact]
    public void WithdrawStake_DropsToZero_SetsInactive()
    {
        Validator v = MakeValidator(stake: new Atomos(1));
        v.WithdrawStake(new Atomos(1));
        Assert.Equal(ValidatorStatus.Inactive, v.Status);
    }

    [Fact]
    public void WithdrawStake_WhenPenalized_Throws()
    {
        Validator v = MakeValidator(stake: new Atomos(1));
        v.ApplyPenalty(new Atomos(1), "evidence");
        Assert.Throws<InvalidOperationException>(() => v.WithdrawStake(Atomos.One));
    }

    [Fact]
    public void ApplyPenalty_DeductsFromStake()
    {
        Validator v = MakeValidator(stake: new Atomos(1000));
        v.ApplyPenalty(new Atomos(300), "approved invalid tx");
        Assert.Equal(new Atomos(700), v.StakedAmount);
        Assert.Equal(new Atomos(300), v.TotalPenaltyAmount);
    }

    [Fact]
    public void ApplyPenalty_ExceedsStake_CapsAtStake()
    {
        Validator v = MakeValidator(stake: new Atomos(100));
        v.ApplyPenalty(new Atomos(500), "evidence");
        Assert.Equal(Atomos.Zero, v.StakedAmount);
        Assert.Equal(new Atomos(100), v.TotalPenaltyAmount);
    }

    [Fact]
    public void ApplyPenalty_DropsStakeBelowOne_SetsPenalized()
    {
        Validator v = MakeValidator(stake: new Atomos(1));
        v.ApplyPenalty(new Atomos(1), "evidence");
        Assert.Equal(ValidatorStatus.Penalized, v.Status);
    }

    [Fact]
    public void ApplyPenalty_ZeroAmount_Throws()
        => Assert.Throws<ArgumentException>(() => MakeValidator().ApplyPenalty(Atomos.Zero, "e"));

    [Fact]
    public void ApplyPenalty_EmptyEvidence_Throws()
        => Assert.Throws<ArgumentException>(() => MakeValidator().ApplyPenalty(new Atomos(1), ""));

    [Fact]
    public void ReceiveReward_IncreasesStakeAndRewards()
    {
        Validator v = MakeValidator(stake: new Atomos(1000));
        v.ReceiveReward(new Atomos(100));
        Assert.Equal(new Atomos(1100), v.StakedAmount);
        Assert.Equal(new Atomos(100), v.TotalRewardsEarned);
    }

    [Fact]
    public void ReceiveReward_WhenPenalized_Throws()
    {
        Validator v = MakeValidator(stake: new Atomos(1));
        v.ApplyPenalty(new Atomos(1), "evidence");
        Assert.Throws<InvalidOperationException>(() => v.ReceiveReward(new Atomos(100)));
    }

    [Fact]
    public void ReceiveReward_WhenInactive_RestoresToActive()
    {
        Validator v = MakeValidator(stake: new Atomos(1));
        v.WithdrawStake(new Atomos(1));
        Assert.Equal(ValidatorStatus.Inactive, v.Status);
        v.AddStake(new Atomos(1));
        Assert.Equal(ValidatorStatus.Active, v.Status);
    }

    [Fact]
    public void GetVotingWeight_ActiveValidator_ReturnsStake()
        => Assert.Equal(new Atomos(1000), MakeValidator(stake: new Atomos(1000)).GetVotingWeight());

    [Fact]
    public void GetVotingWeight_PenalizedValidator_ReturnsZero()
    {
        Validator v = MakeValidator(stake: new Atomos(1));
        v.ApplyPenalty(new Atomos(1), "evidence");
        Assert.Equal(Atomos.Zero, v.GetVotingWeight());
    }

    [Fact]
    public void GetVotingWeight_SuspendedValidator_ReturnsZero()
    {
        Validator v = MakeValidator();
        v.Suspend();
        Assert.Equal(Atomos.Zero, v.GetVotingWeight());
    }

    [Fact]
    public void Warn_FromActive_SetsWarned()
    {
        Validator v = MakeValidator();
        v.Warn();
        Assert.Equal(ValidatorStatus.Warned, v.Status);
    }

    [Fact]
    public void Suspend_FromWarned_SetsSuspended()
    {
        Validator v = MakeValidator();
        v.Warn();
        v.Suspend();
        Assert.Equal(ValidatorStatus.Suspended, v.Status);
    }

    [Fact]
    public void MarkUnreachable_FromActive_SetsUnreachable()
    {
        Validator v = MakeValidator();
        v.MarkUnreachable();
        Assert.Equal(ValidatorStatus.Unreachable, v.Status);
    }

    [Fact]
    public void IsReachable_Active_ReturnsTrue()
        => Assert.True(MakeValidator().IsReachable());

    [Fact]
    public void IsReachable_Penalized_ReturnsFalse()
    {
        Validator v = MakeValidator(stake: new Atomos(1));
        v.ApplyPenalty(new Atomos(1), "evidence");
        Assert.False(v.IsReachable());
    }

    [Fact]
    public void ReputationScore_NoVotes_IsZero()
        => Assert.Equal(0.0, MakeValidator().ReputationScore);

    [Fact]
    public void ReputationScore_AllCorrect_Is100()
    {
        Validator v = MakeValidator();
        v.RecordVote(true);
        v.RecordVote(true);
        Assert.Equal(100.0, v.ReputationScore);
    }

    [Fact]
    public void ReputationScore_HalfCorrect_Is50()
    {
        Validator v = MakeValidator();
        v.RecordVote(true);
        v.RecordVote(false);
        Assert.Equal(50.0, v.ReputationScore);
    }

    [Fact]
    public void RecordValidation_Works()
    {
        Validator v = MakeValidator();
        Guid id = Guid.NewGuid();
        v.RecordValidation(id, new Atomos(500));
        Assert.Contains(id, v.ConfirmedValidationIds);
        Assert.Equal(new BigInteger(1), v.TransactionsValidatedCount);
    }

    [Fact]
    public void RecordValidation_Duplicate_Throws()
    {
        Validator v = MakeValidator();
        Guid id = Guid.NewGuid();
        v.RecordValidation(id, new Atomos(500));
        Assert.Throws<InvalidOperationException>(() => v.RecordValidation(id, new Atomos(500)));
    }

    [Fact]
    public void RecordRejection_Works()
    {
        Validator v = MakeValidator();
        Guid id = Guid.NewGuid();
        v.RecordRejection(id);
        Assert.Contains(id, v.RejectedValidationIds);
        Assert.Equal(new BigInteger(1), v.RejectedTransactionsCount);
    }

    [Fact]
    public void UpdateTier_ChangesToCore()
    {
        Validator v = MakeValidator();
        v.UpdateTier(ValidatorTier.Core);
        Assert.Equal(ValidatorTier.Core, v.Tier);
    }
}
