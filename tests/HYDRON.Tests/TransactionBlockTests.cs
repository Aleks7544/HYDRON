using System.Numerics;
using HYDRON.Models;
using Xunit;

namespace HYDRON.Tests;

public class TransactionBlockTests
{
    private static TransactionBlock MakeBlock() =>
        new(BigInteger.One, "prev_hash", "producer_addr", new Atomos(42));

    private static Transaction MakeFinalizedTx(
        string sender = "alice", string receiver = "bob", char hashFill = 'a')
    {
        Transaction tx = new Transaction(sender, receiver, new Atomos(1000), new Atomos(100),
            BigInteger.One, "sig_sender", false);
        tx.SetHash(new string(hashFill, 64));
        tx.UpdateStatus(TransactionStatus.PendingValidation);
        tx.UpdateStatus(TransactionStatus.ConsensusReached);
        tx.UpdateStatus(TransactionStatus.Settled);
        tx.FinalizeTransaction();
        return tx;
    }

    [Fact]
    public void Constructor_ValidArgs_InitialisesCorrectly()
    {
        TransactionBlock b = MakeBlock();
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

    [Fact]
    public void AddTransaction_ValidTx_IncreasesCount()
    {
        TransactionBlock b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        Assert.Equal(1, b.TransactionCount);
    }

    [Fact]
    public void AddTransaction_NullTx_Throws()
        => Assert.Throws<ArgumentNullException>(() => MakeBlock().AddTransaction(null!));

    [Fact]
    public void AddTransaction_NotFinalized_Throws()
    {
        TransactionBlock b = MakeBlock();
        Transaction tx = new Transaction("alice", "bob", new Atomos(1000), new Atomos(100),
            BigInteger.One, "sig", false);
        Assert.Throws<InvalidOperationException>(() => b.AddTransaction(tx));
    }

    [Fact]
    public void AddTransaction_NoHash_Throws()
    {
        TransactionBlock b = MakeBlock();
        Transaction tx = new Transaction("alice", "bob", new Atomos(1000), new Atomos(100),
            BigInteger.One, "sig", false);
        tx.UpdateStatus(TransactionStatus.AbortedBySender);
        tx.FinalizeTransaction();
        Assert.Throws<InvalidOperationException>(() => b.AddTransaction(tx));
    }

    [Fact]
    public void AddTransaction_Duplicate_Throws()
    {
        TransactionBlock b = MakeBlock();
        Transaction tx = MakeFinalizedTx();
        b.AddTransaction(tx);
        Assert.Throws<InvalidOperationException>(() => b.AddTransaction(tx));
    }

    [Fact]
    public void AddTransaction_AfterSealed_Throws()
    {
        TransactionBlock b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx(hashFill: 'a'));
        b.Seal(new string('b', 64), new string('c', 64), "state_root");
        Assert.Throws<InvalidOperationException>(() =>
            b.AddTransaction(MakeFinalizedTx("carol", "dave", 'd')));
    }

    [Fact]
    public void GetTotalFees_SumsAllFees()
    {
        TransactionBlock b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx("alice", "bob", 'a'));
        b.AddTransaction(MakeFinalizedTx("carol", "dave", 'b'));
        Assert.Equal(new Atomos(200), b.GetTotalFees());
    }

    [Fact]
    public void GetTotalFees_EmptyBlock_ReturnsZero()
        => Assert.Equal(Atomos.Zero, MakeBlock().GetTotalFees());

    [Fact]
    public void Seal_WithTransaction_SetsIsSealed()
    {
        TransactionBlock b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        b.Seal(new string('b', 64), new string('c', 64), "state_root");
        Assert.True(b.IsSealed);
        Assert.True(b.IsValid);
    }

    [Fact]
    public void Seal_EmptyBlock_Throws()
        => Assert.Throws<InvalidOperationException>(() =>
            MakeBlock().Seal(new string('b', 64), new string('c', 64), "state_root"));

    [Fact]
    public void Seal_EmptyHash_Throws()
    {
        TransactionBlock b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        Assert.Throws<ArgumentException>(() => b.Seal("", "merkle", "state"));
    }

    [Fact]
    public void Seal_EmptyMerkleRoot_Throws()
    {
        TransactionBlock b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        Assert.Throws<ArgumentException>(() => b.Seal("hash", "", "state"));
    }

    [Fact]
    public void Seal_EmptyStateRoot_Throws()
    {
        TransactionBlock b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        Assert.Throws<ArgumentException>(() => b.Seal("hash", "merkle", ""));
    }

    [Fact]
    public void Seal_Twice_Throws()
    {
        TransactionBlock b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        b.Seal(new string('b', 64), new string('c', 64), "state_root");
        Assert.Throws<InvalidOperationException>(() =>
            b.Seal(new string('d', 64), new string('e', 64), "state_root2"));
    }

    [Fact]
    public void IsValid_BeforeSeal_ReturnsFalse()
    {
        TransactionBlock b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        Assert.False(b.IsValid);
    }

    [Fact]
    public void IsValid_AfterSeal_ReturnsTrue()
    {
        TransactionBlock b = MakeBlock();
        b.AddTransaction(MakeFinalizedTx());
        b.Seal(new string('b', 64), new string('c', 64), "state_root");
        Assert.True(b.IsValid);
    }
}
