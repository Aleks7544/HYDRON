using System.Numerics;
using HYDRON.Models;

namespace HYDRON.Tests;

public class TransactionTests
{
    private static Transaction MakeTx(
        string sender = "alice",
        string receiver = "bob",
        Atomos? amount = null,
        bool requiresReceiver = false) =>
        new(sender, receiver, amount ?? new Atomos(1000), new Atomos(100),
            BigInteger.One, "sig_sender", requiresReceiver);

    // --- Construction guards ---

    [Fact]
    public void Constructor_SameSenderReceiver_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new Transaction("alice", "alice", new Atomos(1), Atomos.Zero, BigInteger.One, "sig", false));

    [Fact]
    public void Constructor_ZeroAmount_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new Transaction("alice", "bob", Atomos.Zero, Atomos.Zero, BigInteger.One, "sig", false));

    [Fact]
    public void Constructor_EmptySenderSignature_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new Transaction("alice", "bob", new Atomos(1), Atomos.Zero, BigInteger.One, "", false));

    [Fact]
    public void Constructor_PrivacyModeNonPublicWithoutEphemeralKey_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new Transaction("alice", "bob", new Atomos(1), Atomos.Zero, BigInteger.One, "sig", false,
                privacyMode: PrivacyMode.FullyPrivate, ephemeralPublicKey: null));

    // --- Status lifecycle ---

    [Fact]
    public void InitialStatus_IsInitiatedBySender()
        => Assert.Equal(TransactionStatus.InitiatedBySender, MakeTx().Status);

    [Fact]
    public void UpdateStatus_ValidTransition_Works()
    {
        var tx = MakeTx();
        tx.UpdateStatus(TransactionStatus.PendingValidation);
        Assert.Equal(TransactionStatus.PendingValidation, tx.Status);
    }

    [Fact]
    public void UpdateStatus_InvalidTransition_Throws()
    {
        var tx = MakeTx();
        Assert.Throws<InvalidOperationException>(() =>
            tx.UpdateStatus(TransactionStatus.Settled));
    }

    [Fact]
    public void UpdateStatus_AfterFinalize_Throws()
    {
        var tx = MakeTx();
        tx.UpdateStatus(TransactionStatus.AbortedBySender);
        tx.FinalizeTransaction();
        Assert.Throws<InvalidOperationException>(() =>
            tx.UpdateStatus(TransactionStatus.Rejected));
    }

    // --- Finalization ---

    [Fact]
    public void FinalizeTransaction_FromTerminalStatus_Works()
    {
        var tx = MakeTx();
        tx.UpdateStatus(TransactionStatus.AbortedBySender);
        tx.FinalizeTransaction();
        Assert.True(tx.IsFinalized);
        Assert.NotNull(tx.FinalizedAt);
    }

    [Fact]
    public void FinalizeTransaction_FromNonTerminalStatus_Throws()
    {
        var tx = MakeTx();
        Assert.Throws<InvalidOperationException>(() => tx.FinalizeTransaction());
    }

    [Fact]
    public void FinalizeTransaction_AlreadyFinalized_Throws()
    {
        var tx = MakeTx();
        tx.UpdateStatus(TransactionStatus.AbortedBySender);
        tx.FinalizeTransaction();
        Assert.Throws<InvalidOperationException>(() => tx.FinalizeTransaction());
    }

    // --- Hash ---

    [Fact]
    public void SetHash_CanBeSetOnce()
    {
        var tx = MakeTx();
        tx.SetHash("abc123");
        Assert.Equal("abc123", tx.Hash);
    }

    [Fact]
    public void SetHash_CannotBeSetTwice()
    {
        var tx = MakeTx();
        tx.SetHash("abc123");
        Assert.Throws<InvalidOperationException>(() => tx.SetHash("xyz"));
    }

    // --- Receiver signature ---

    [Fact]
    public void SetReceiverSignature_WhenRequired_Works()
    {
        var tx = MakeTx(requiresReceiver: true);
        tx.SetReceiverSignature("recv_sig");
        Assert.True(tx.IsSignedByReceiver());
    }

    [Fact]
    public void SetReceiverSignature_WhenNotRequired_Throws()
        => Assert.Throws<InvalidOperationException>(() => MakeTx().SetReceiverSignature("sig"));

    [Fact]
    public void SetReceiverSignature_Twice_Throws()
    {
        var tx = MakeTx(requiresReceiver: true);
        tx.SetReceiverSignature("recv_sig");
        Assert.Throws<InvalidOperationException>(() => tx.SetReceiverSignature("another_sig"));
    }

    // --- Validators ---

    [Fact]
    public void AddValidator_BeforePending_Works()
    {
        var tx = MakeTx();
        tx.AddValidator("val1");
        Assert.Contains("val1", tx.AssignedValidators);
    }

    [Fact]
    public void AddValidator_DuplicateAddress_Throws()
    {
        var tx = MakeTx();
        tx.AddValidator("val1");
        Assert.Throws<InvalidOperationException>(() => tx.AddValidator("val1"));
    }

    [Fact]
    public void AddValidator_AfterPendingValidation_Throws()
    {
        var tx = MakeTx();
        tx.AddValidator("val1");
        tx.UpdateStatus(TransactionStatus.PendingValidation);
        Assert.Throws<InvalidOperationException>(() => tx.AddValidator("val2"));
    }

    [Fact]
    public void RemoveValidator_BeforeFreeze_Works()
    {
        var tx = MakeTx();
        tx.AddValidator("val1");
        tx.RemoveValidator("val1");
        Assert.DoesNotContain("val1", tx.AssignedValidators);
    }

    [Fact]
    public void RemoveValidator_AfterFreeze_Throws()
    {
        var tx = MakeTx();
        tx.AddValidator("val1");
        tx.UpdateStatus(TransactionStatus.PendingValidation); // freezes
        Assert.Throws<InvalidOperationException>(() => tx.RemoveValidator("val1"));
    }

    // --- GetTotalCost ---

    [Fact]
    public void GetTotalCost_ReturnsSumOfAmountAndFee()
    {
        var tx = MakeTx(amount: new Atomos(1000));
        Assert.Equal(new Atomos(1100), tx.GetTotalCost());
    }

    // --- Priority ---

    [Fact]
    public void ChangePriority_BeforePendingValidation_Works()
    {
        var tx = MakeTx();
        tx.ChangePriority(Priority.High);
        Assert.Equal(Priority.High, tx.Priority);
    }

    [Fact]
    public void ChangePriority_AfterPendingValidation_Throws()
    {
        var tx = MakeTx();
        tx.UpdateStatus(TransactionStatus.PendingValidation);
        Assert.Throws<InvalidOperationException>(() => tx.ChangePriority(Priority.High));
    }

    // --- AssignBlockNumber ---

    [Fact]
    public void AssignBlockNumber_WhenSettled_Works()
    {
        var tx = MakeTx();
        tx.UpdateStatus(TransactionStatus.PendingValidation);
        tx.UpdateStatus(TransactionStatus.ConsensusReached);
        tx.UpdateStatus(TransactionStatus.Settled);
        tx.AssignBlockNumber(new BigInteger(42));
        Assert.Equal(new BigInteger(42), tx.TransactionBlockNumber);
    }

    [Fact]
    public void AssignBlockNumber_BeforeSettled_Throws()
    {
        var tx = MakeTx();
        Assert.Throws<InvalidOperationException>(() => tx.AssignBlockNumber(BigInteger.One));
    }
}
