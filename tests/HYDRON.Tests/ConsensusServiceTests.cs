using System.Numerics;
using HYDRON.Models;
using HYDRON.Validator;

namespace HYDRON.Tests;

public class ConsensusServiceTests
{
    private const string TxHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private static Models.Validator MakeValidator(string address) =>
        new(address, "pubkey-" + address, "stealth-" + address,
            new Atomos(10_000), networkEndpointIPv4: "127.0.0.1");

    private static Transaction MakePendingTx(IEnumerable<string> validatorAddresses)
    {
        var tx = new Transaction("alice", "bob",
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
        var addresses = Enumerable.Range(1, validatorCount)
            .Select(i => $"v{i}").ToList();
        var validators = addresses.Select(MakeValidator).ToList();
        var tx = MakePendingTx(addresses);
        var svc = new ConsensusService(tx, validators);
        return (svc, tx, validators);
    }

    // --- Construction guards ---

    [Fact]
    public void Constructor_NullTx_Throws()
        => Assert.Throws<ArgumentNullException>(() =>
            new ConsensusService(null!, [MakeValidator("v1")]));

    [Fact]
    public void Constructor_NullValidators_Throws()
    {
        var tx = MakePendingTx(["v1"]);
        Assert.Throws<ArgumentNullException>(() => new ConsensusService(tx, null!));
    }

    [Fact]
    public void Constructor_EmptyValidators_Throws()
    {
        var tx = MakePendingTx(["v1"]);
        Assert.Throws<ArgumentException>(() => new ConsensusService(tx, []));
    }

    [Fact]
    public void Constructor_TxNotPendingValidation_Throws()
    {
        var tx = new Transaction("alice", "bob",
            new Atomos(1000), new Atomos(100),
            BigInteger.One, "sig", false);
        tx.SetHash(TxHash);
        // Status is InitiatedBySender — not PendingValidation
        Assert.Throws<ArgumentException>(() =>
            new ConsensusService(tx, [MakeValidator("v1")]));
    }

    [Fact]
    public void Constructor_ValidArgs_ResultIsPending()
    {
        var (svc, _, _) = MakeService();
        Assert.Equal(ConsensusResult.Pending, svc.Result);
    }

    // --- SubmitVote guards ---

    [Fact]
    public void SubmitVote_EmptyAddress_Throws()
    {
        var (svc, _, _) = MakeService();
        Assert.Throws<ArgumentException>(() => svc.SubmitVote("", true, "sig", 100.0));
    }

    [Fact]
    public void SubmitVote_EmptySignature_Throws()
    {
        var (svc, _, _) = MakeService();
        Assert.Throws<ArgumentException>(() => svc.SubmitVote("v1", true, "", 100.0));
    }

    [Fact]
    public void SubmitVote_NegativeSpeed_Throws()
    {
        var (svc, _, _) = MakeService();
        Assert.Throws<ArgumentException>(() => svc.SubmitVote("v1", true, "sig", -1.0));
    }

    [Fact]
    public void SubmitVote_UnassignedValidator_Throws()
    {
        var (svc, _, _) = MakeService();
        Assert.Throws<InvalidOperationException>(() =>
            svc.SubmitVote("unknown", true, "sig", 100.0));
    }

    [Fact]
    public void SubmitVote_AfterConsensusReached_Throws()
    {
        var (svc, _, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 100.0);
        svc.SubmitVote("v2", true, "sig", 100.0); // supermajority reached
        Assert.Throws<InvalidOperationException>(() =>
            svc.SubmitVote("v3", true, "sig", 100.0));
    }

    // --- Full approval flow ---

    [Fact]
    public void FullApprovalFlow_2of3_ResultIsApproved()
    {
        var (svc, _, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 50.0);
        svc.SubmitVote("v2", true, "sig", 60.0);
        Assert.Equal(ConsensusResult.Approved, svc.Result);
    }

    [Fact]
    public void FullApprovalFlow_TryFinalize_TransitionToConsensusReached()
    {
        var (svc, tx, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 50.0);
        svc.SubmitVote("v2", true, "sig", 60.0);
        Assert.True(svc.TryFinalize());
        Assert.Equal(TransactionStatus.ConsensusReached, tx.Status);
    }

    [Fact]
    public void FullApprovalFlow_ApproversListCorrect()
    {
        var (svc, _, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 50.0);
        svc.SubmitVote("v2", true, "sig", 60.0);
        Assert.Contains("v1", svc.Approvers);
        Assert.Contains("v2", svc.Approvers);
    }

    // --- Full rejection flow ---

    [Fact]
    public void FullRejectionFlow_2of3_ResultIsRejected()
    {
        var (svc, _, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 50.0); // first validator approves — no veto
        svc.SubmitVote("v2", false, "sig", 60.0);
        svc.SubmitVote("v3", false, "sig", 70.0);
        Assert.Equal(ConsensusResult.Rejected, svc.Result);
    }

    [Fact]
    public void FullRejectionFlow_TryFinalize_TransitionToRejected()
    {
        var (svc, tx, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 50.0);
        svc.SubmitVote("v2", false, "sig", 60.0);
        svc.SubmitVote("v3", false, "sig", 70.0);
        Assert.True(svc.TryFinalize());
        Assert.Equal(TransactionStatus.Rejected, tx.Status);
    }

    // --- Veto flow ---

    [Fact]
    public void VetoFlow_FirstValidatorRejects_InstantVeto()
    {
        var (svc, _, _) = MakeService(5);
        svc.SubmitVote("v1", false, "sig", 30.0);
        Assert.Equal(ConsensusResult.VetoedByFirstValidator, svc.Result);
    }

    [Fact]
    public void VetoFlow_TryFinalize_TransitionToRejected()
    {
        var (svc, tx, _) = MakeService(5);
        svc.SubmitVote("v1", false, "sig", 30.0);
        Assert.True(svc.TryFinalize());
        Assert.Equal(TransactionStatus.Rejected, tx.Status);
    }

    [Fact]
    public void VetoFlow_RejectersContainsFirstValidator()
    {
        var (svc, _, _) = MakeService(5);
        svc.SubmitVote("v1", false, "sig", 30.0);
        Assert.Contains("v1", svc.Rejecters);
    }

    // --- TryFinalize on Pending ---

    [Fact]
    public void TryFinalize_WhenPending_ReturnsFalse()
    {
        var (svc, tx, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 50.0); // only 1 of 3 — still pending
        Assert.False(svc.TryFinalize());
        Assert.Equal(TransactionStatus.PendingValidation, tx.Status);
    }

    // --- Validation is recorded on tx ---

    [Fact]
    public void SubmitVote_ValidationsRecordedOnTransaction()
    {
        var (svc, tx, _) = MakeService(3);
        svc.SubmitVote("v1", true, "sig", 50.0);
        svc.SubmitVote("v2", false, "sig", 60.0);
        Assert.Equal(2, tx.RegisteredValidationIds.Count);
    }
}
