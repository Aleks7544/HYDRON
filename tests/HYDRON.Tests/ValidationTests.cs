using HYDRON.Models;
using Xunit;

namespace HYDRON.Tests;

public class ValidationTests
{
    private static Validation MakeValidation() =>
        new("tx_hash_abc", "validator_addr");

    [Fact]
    public void Constructor_ValidArgs_InitialisesCorrectly()
    {
        Validation v = MakeValidation();
        Assert.Equal("tx_hash_abc", v.TransactionHash);
        Assert.Equal("validator_addr", v.ValidatorAddress);
        Assert.Equal(ValidationStatus.Pending, v.Status);
        Assert.Null(v.ValidationSignature);
        Assert.False(v.IsPenalized);
    }

    [Theory]
    [InlineData("", "addr")]
    [InlineData("hash", "")]
    public void Constructor_EmptyArgs_Throws(string hash, string addr)
        => Assert.Throws<ArgumentException>(() => new Validation(hash, addr));

    [Fact]
    public void SignValidation_Works()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig_abc");
        Assert.Equal("sig_abc", v.ValidationSignature);
    }

    [Fact]
    public void SignValidation_EmptySignature_Throws()
        => Assert.Throws<ArgumentException>(() => MakeValidation().SignValidation(""));

    [Fact]
    public void SignValidation_Twice_Throws()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig1");
        Assert.Throws<InvalidOperationException>(() => v.SignValidation("sig2"));
    }

    [Fact]
    public void SignValidation_AfterConfirm_Throws()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig");
        v.Confirm(10.0);
        Assert.Throws<InvalidOperationException>(() => v.SignValidation("sig2"));
    }

    [Fact]
    public void Confirm_WithoutSignature_Throws()
        => Assert.Throws<InvalidOperationException>(() => MakeValidation().Confirm(10.0));

    [Fact]
    public void Confirm_WithSignature_SetsStatusAndTimestamp()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig");
        v.Confirm(42.5);
        Assert.Equal(ValidationStatus.Confirmed, v.Status);
        Assert.Equal(42.5, v.ValidationSpeedMs);
        Assert.NotNull(v.ValidatedAt);
    }

    [Fact]
    public void Confirm_NegativeSpeed_Throws()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig");
        Assert.Throws<ArgumentException>(() => v.Confirm(-1.0));
    }

    [Fact]
    public void Confirm_AlreadyConfirmed_Throws()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig");
        v.Confirm(10.0);
        Assert.Throws<InvalidOperationException>(() => v.Confirm(10.0));
    }

    [Fact]
    public void Reject_WithoutSignature_Throws()
        => Assert.Throws<InvalidOperationException>(() => MakeValidation().Reject(10.0));

    [Fact]
    public void Reject_WithSignature_SetsStatusAndTimestamp()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig");
        v.Reject(15.0);
        Assert.Equal(ValidationStatus.Rejected, v.Status);
        Assert.Equal(15.0, v.ValidationSpeedMs);
        Assert.NotNull(v.ValidatedAt);
    }

    [Fact]
    public void Reject_AlreadyRejected_Throws()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig");
        v.Reject(10.0);
        Assert.Throws<InvalidOperationException>(() => v.Reject(10.0));
    }

    [Fact]
    public void AssignReward_ToConfirmed_Works()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig");
        v.Confirm(10.0);
        v.AssignReward(new Atomos(100));
        Assert.Equal(new Atomos(100), v.FeeReward);
    }

    [Fact]
    public void AssignReward_ToPending_Throws()
        => Assert.Throws<InvalidOperationException>(() => MakeValidation().AssignReward(new Atomos(100)));

    [Fact]
    public void AssignReward_ToRejected_Throws()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig");
        v.Reject(10.0);
        Assert.Throws<InvalidOperationException>(() => v.AssignReward(new Atomos(100)));
    }

    [Fact]
    public void AssignReward_Twice_Throws()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig");
        v.Confirm(10.0);
        v.AssignReward(new Atomos(100));
        Assert.Throws<InvalidOperationException>(() => v.AssignReward(new Atomos(50)));
    }

    [Fact]
    public void Penalize_OnConfirmed_Works()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig");
        v.Confirm(10.0);
        v.Penalize(new Atomos(500), "approved invalid tx");
        Assert.True(v.IsPenalized);
        Assert.Equal(new Atomos(500), v.PenaltyAmount);
        Assert.NotNull(v.PenaltyTimestamp);
    }

    [Fact]
    public void Penalize_OnRejected_Works()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig");
        v.Reject(10.0);
        v.Penalize(new Atomos(200), "rejected valid tx");
        Assert.True(v.IsPenalized);
    }

    [Fact]
    public void Penalize_OnPending_Throws()
        => Assert.Throws<InvalidOperationException>(() =>
            MakeValidation().Penalize(new Atomos(100), "evidence"));

    [Fact]
    public void Penalize_Twice_Throws()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig");
        v.Confirm(10.0);
        v.Penalize(new Atomos(100), "first");
        Assert.Throws<InvalidOperationException>(() => v.Penalize(new Atomos(50), "second"));
    }

    [Fact]
    public void Penalize_ZeroAmount_Throws()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig");
        v.Confirm(10.0);
        Assert.Throws<ArgumentException>(() => v.Penalize(Atomos.Zero, "evidence"));
    }

    [Fact]
    public void Penalize_EmptyEvidence_Throws()
    {
        Validation v = MakeValidation();
        v.SignValidation("sig");
        v.Confirm(10.0);
        Assert.Throws<ArgumentException>(() => v.Penalize(new Atomos(100), ""));
    }
}
