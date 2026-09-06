using System.Numerics;
using HYDRON.Models;

namespace HYDRON.Tests;

public class TransactionBlockTests
{
    private static TransactionBlock MakeBlock() =>
        new(BigInteger.One, "prev_hash", "producer_addr", new Atomos(42));

    private static Transaction MakeFinalizedTx(string sender = "alice", string receiver = "bob")
    {
        var tx = new Transaction(sender, receiver, new Atomos(1000), new Atomos(100),
            BigInteger.One, "sig_sender", false);
        tx.SetHash("a" + new string('0', 63)); // 64-char hash
        tx.UpdateStatus(TransactionStatus.PendingValidation);
        tx.UpdateStatus(TransactionStatus.ConsensusReached);
        tx.UpdateStatus(TransactionStatus.Settled);
        tx.FinalizeTransaction();
        return tx;
    }

    // --- Construction ---

    [Fact]
    public void Constructor_ValidArgs_InitialisesCorrectly()
    {
        var b = MakeBlock();
        Assert.Equal(BigInteger.One, b.BlockNumber);
        Assert.Equal("prev_hash", b.PreviousHash);
        Assert.Equal("producer_addr", b.ProducerAddress);
        Assert.Equal(new Atomos(42), b.ElectricityPriceAtomosPerEv);
        Assert.Equal(0, b.TransactionCount);
        Assert.False(b.IsSealed);
        Assert.False(b.IsValid);
    }

    [Fact]
    public void Constructor_NegativeBlockNumber_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new TransactionBlock(new BigInteger(-1), "prev", "prod", new Atomos(1)));

    [Fact]
    public void Constructor_EmptyPreviousHash_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new TransactionBlock(BigInteger.Zero, "", "prod", new Atomos(1)));

    [Fact]
    public void Constructor_EmptyProducerAddress_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new TransactionBlock(BigInteger.Zero, "prev", "", new Atomos(1)));

    [Fact]
    public void Constructor_ZeroElectricityPrice_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new TransactionBlock(BigInteger.Zero, "prev", "prod", Atomos.Zero));

    // --- AddTransaction ---

    [Fact]
    public void AddTransaction_ValidTx_IncreasesCount()
    {
        var b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        Assert.Equal(1, b.TransactionCount);
    }

    [Fact]
    public void AddTransaction_NullTx_Throws()
        => Assert.Throws<ArgumentNullException>(() => MakeBlock().AddTransaction(null!));

    [Fact]
    public void AddTransaction_NotFinalized_Throws()
    {
        var b = MakeBlock();
        var tx = new Transaction("alice", "bob", new Atomos(1000), new Atomos(100),
            BigInteger.One, "sig", false);
        Assert.Throws<InvalidOperationException>(() => b.AddTransaction(tx));
    }

    [Fact]
    public void AddTransaction_NoHash_Throws()
    {
        var b = MakeBlock();
        var tx = new Transaction("alice", "bob", new Atomos(1000), new Atomos(100),
            BigInteger.One, "sig", false);
        tx.UpdateStatus(TransactionStatus.AbortedBySender);
        tx.FinalizeTransaction();
        Assert.Throws<InvalidOperationException>(() => b.AddTransaction(tx));
    }

    [Fact]
    public void AddTransaction_Duplicate_Throws()
    {
        var b = MakeBlock();
        var tx = MakeFinalizedTx();
        b.AddTransaction(tx);
        Assert.Throws<InvalidOperationException>(() => b.AddTransaction(tx));
    }

    [Fact]
    public void AddTransaction_AfterSealed_Throws()
    {
        var b = MakeBlock();
        var tx = MakeFinalizedTx();
        b.AddTransaction(tx);
        b.Seal("b" + new string('0', 63), "c" + new string('0', 63), "state_root");
        Assert.Throws<InvalidOperationException>(() => b.AddTransaction(MakeFinalizedTx("carol", "dave")));
    }

    // --- GetTotalFees ---

    [Fact]
    public void GetTotalFees_SumsAllFees()
    {
        var b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx("alice", "bob"));
        b.AddTransaction(MakeFinalizedTx("carol", "dave"));
        Assert.Equal(new Atomos(200), b.GetTotalFees());
    }

    [Fact]
    public void GetTotalFees_EmptyBlock_ReturnsZero()
        => Assert.Equal(Atomos.Zero, MakeBlock().GetTotalFees());

    // --- Seal ---

    [Fact]
    public void Seal_WithTransaction_SetsIsSealed()
    {
        var b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        b.Seal("b" + new string('0', 63), "c" + new string('0', 63), "state_root");
        Assert.True(b.IsSealed);
        Assert.True(b.IsValid);
    }

    [Fact]
    public void Seal_EmptyBlock_Throws()
        => Assert.Throws<InvalidOperationException>(() =>
            MakeBlock().Seal("b" + new string('0', 63), "c" + new string('0', 63), "state_root"));

    [Fact]
    public void Seal_EmptyHash_Throws()
    {
        var b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        Assert.Throws<ArgumentException>(() => b.Seal("", "merkle", "state"));
    }

    [Fact]
    public void Seal_EmptyMerkleRoot_Throws()
    {
        var b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        Assert.Throws<ArgumentException>(() => b.Seal("hash", "", "state"));
    }

    [Fact]
    public void Seal_EmptyStateRoot_Throws()
    {
        var b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        Assert.Throws<ArgumentException>(() => b.Seal("hash", "merkle", ""));
    }

    [Fact]
    public void Seal_Twice_Throws()
    {
        var b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        b.Seal("b" + new string('0', 63), "c" + new string('0', 63), "state_root");
        Assert.Throws<InvalidOperationException>(() =>
            b.Seal("d" + new string('0', 63), "e" + new string('0', 63), "state_root2"));
    }

    // --- IsValid ---

    [Fact]
    public void IsValid_BeforeSeal_ReturnsFalse()
    {
        var b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        Assert.False(b.IsValid);
    }

    [Fact]
    public void IsValid_AfterSeal_ReturnsTrue()
    {
        var b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        b.Seal("b" + new string('0', 63), "c" + new string('0', 63), "state_root");
        Assert.True(b.IsValid);
    }
}
