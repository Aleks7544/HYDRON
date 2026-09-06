using System.Numerics;
using HYDRON.Models;
using HYDRON.Validator;
using Xunit;

namespace HYDRON.Tests;

public class ConsensusServiceTests
{
    private const string TxHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private static Models.Validator MakeValidator(string address) =>
        new(address, "pubkey-" + address, "stealth-" + address,
            new Atomos(10_000), networkEndpointIPv4: "127.0.0.1");

    private static Transaction MakePendingTx(IEnumerable<string> validatorAddresses)
    {
        Transaction tx = new Transaction("alice", "bob",
            new Atomos(1000), new Atomos(100),
            BigInteger.One, "sig", false);
        tx.SetHash(TxHash);
        foreach (string addr in validatorAddresses)
            tx.AddValidator(addr);
        tx.UpdateStatus(TransactionStatus.PendingValidation);
        return tx;
    }

    private static (ConsensusService svc, Transaction tx, List<Models.Validator> validators)
        MakeService(int validatorCount = 3)
    {
        List<string> addresses = Enumerable.Range(1, validatorCount)
            .Select(i => $"v{i}").ToList();
        List<Models.Validator> validators = addresses.Select(MakeValidator).ToList();
        Transaction tx = MakePendingTx(addresses);
        ConsensusService svc = new ConsensusService(tx, validators);
        return (svc, tx, validators);
    }

    [Fact]
    public void Constructor_NullTx_Throws()
        => Assert.Throws<ArgumentNullException>(() =>
            new ConsensusService(null!, [MakeValidator("v1")]));

    [Fact]
    public void Constructor_NullValidators_Throws()
    {
        Transaction tx = MakePendingTx(["v1"]);
        Assert.Throws<ArgumentNullException>(() => new ConsensusService(tx, null!));
    }

    [Fact]
    public void Constructor_EmptyValidators_Throws()
    {
        Transaction tx = MakePendingTx(["v1"]);
        Assert.Throws<ArgumentException>(() => new ConsensusService(tx, []));
    }

    [Fact]
    public void Constructor_TxNotPendingValidation_Throws()
    {
        Transaction tx = new Transaction("alice", "bob",
            new Atomos(1000), new Atomos(100),
            BigInteger.One, "sig", false);
        tx.SetHash(TxHash);
        Assert.Throws<ArgumentException>(() =>
            new ConsensusService(tx, [MakeValidator("v1")]));
    }

    [Fact]
    public void Constructor_ValidArgs_ResultIsPending()
    {
        (ConsensusService svc, _, _) = MakeService();
        Assert.Equal(ConsensusResult.Pending, svc.Result);
    }

    [Fact]
    public void SubmitVote_EmptyAddress_Throws()
    {
        (ConsensusService svc, _, _) = MakeService();
        Assert.Throws<ArgumentException>(() => svc.SubmitVote("", true, "sig", 100.0));
    }

    [Fact]
    public void SubmitVote_EmptySignature_Throws()
    {
        (ConsensusService svc, _, _) = MakeService();
        Assert.Throws<ArgumentException>(() => svc.SubmitVote("v1", true, "", 100.0));
    }

    [Fact]
    public void SubmitVote_NegativeSpeed_Throws()
    {
        (ConsensusService svc, _, _) = MakeService();
        Assert.Throws<ArgumentException>(() => svc.SubmitVote("v1", true, "sig", -1.0));
    }

    [Fact]
    public void SubmitVote_UnassignedValidator_Throws()
    {
        (ConsensusService svc, _, _) = MakeService();
        Assert.Throws<InvalidOperationException>(() =>
            svc.SubmitVote("unknown", true, "sig", 100.0));
    }

    [Fact]
    public void SubmitVote_AfterConsensusReached_Throws()
    {
        (ConsensusService svc, _, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 100.0);
        svc.SubmitVote("v2", true, "sig", 100.0); // supermajority reached
        Assert.Throws<InvalidOperationException>(() =>
            svc.SubmitVote("v3", true, "sig", 100.0));
    }

    [Fact]
    public void FullApprovalFlow_2of3_ResultIsApproved()
    {
        (ConsensusService svc, _, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 50.0);
        svc.SubmitVote("v2", true, "sig", 60.0);
        Assert.Equal(ConsensusResult.Approved, svc.Result);
    }

    [Fact]
    public void FullApprovalFlow_TryFinalize_TransitionToConsensusReached()
    {
        (ConsensusService svc, Transaction tx, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 50.0);
        svc.SubmitVote("v2", true, "sig", 60.0);
        Assert.True(svc.TryFinalize());
        Assert.Equal(TransactionStatus.ConsensusReached, tx.Status);
    }

    [Fact]
    public void FullApprovalFlow_ApproversListCorrect()
    {
        (ConsensusService svc, _, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 50.0);
        svc.SubmitVote("v2", true, "sig", 60.0);
        Assert.Contains("v1", svc.Approvers);
        Assert.Contains("v2", svc.Approvers);
    }

    [Fact]
    public void FullRejectionFlow_2of3_ResultIsRejected()
    {
        (ConsensusService svc, _, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 50.0); // first validator approves — no veto
        svc.SubmitVote("v2", false, "sig", 60.0);
        svc.SubmitVote("v3", false, "sig", 70.0);
        Assert.Equal(ConsensusResult.Rejected, svc.Result);
    }

    [Fact]
    public void FullRejectionFlow_TryFinalize_TransitionToRejected()
    {
        (ConsensusService svc, Transaction tx, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 50.0);
        svc.SubmitVote("v2", false, "sig", 60.0);
        svc.SubmitVote("v3", false, "sig", 70.0);
        Assert.True(svc.TryFinalize());
        Assert.Equal(TransactionStatus.Rejected, tx.Status);
    }

    [Fact]
    public void VetoFlow_FirstValidatorRejects_InstantVeto()
    {
        (ConsensusService svc, _, _) = MakeService(5);
        svc.SubmitVote("v1", false, "sig", 30.0);
        Assert.Equal(ConsensusResult.VetoedByFirstValidator, svc.Result);
    }

    [Fact]
    public void VetoFlow_TryFinalize_TransitionToRejected()
    {
        (ConsensusService svc, Transaction tx, _) = MakeService(5);
        svc.SubmitVote("v1", false, "sig", 30.0);
        Assert.True(svc.TryFinalize());
        Assert.Equal(TransactionStatus.Rejected, tx.Status);
    }

    [Fact]
    public void VetoFlow_RejectersContainsFirstValidator()
    {
        (ConsensusService svc, _, _) = MakeService(5);
        svc.SubmitVote("v1", false, "sig", 30.0);
        Assert.Contains("v1", svc.Rejecters);
    }

    [Fact]
    public void TryFinalize_WhenPending_ReturnsFalse()
    {
        (ConsensusService svc, Transaction tx, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 50.0); // only 1 of 3 — still pending
        Assert.False(svc.TryFinalize());
        Assert.Equal(TransactionStatus.PendingValidation, tx.Status);
    }

    [Fact]
    public void SubmitVote_ValidationsRecordedOnTransaction()
    {
        (ConsensusService svc, Transaction tx, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 50.0);
        svc.SubmitVote("v2", false, "sig", 60.0);
        Assert.Equal(2, tx.RegisteredValidationIds.Count);
    }
}
