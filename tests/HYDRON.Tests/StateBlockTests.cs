using System.Numerics;
using HYDRON.Models;

namespace HYDRON.Tests;

public class StateBlockTests
{
    private static StateBlock MakeBlock() =>
        new(BigInteger.One, "prev_hash", "producer_addr");

    private static string MakeHash(char fill) => new(fill, 64);

    private static void FillWithHashes(StateBlock b, int count)
    {
        for (int i = 0; i < count; i++)
            b.AddTransactionBlockHash(MakeHash((char)('a' + (i % 26))) + i.ToString().PadLeft(0));
    }

    // use unique hashes to avoid duplicate rejection
    private static void FillToCapacity(StateBlock b)
    {
        for (int i = 0; i < Block.Capacity; i++)
            b.AddTransactionBlockHash(i.ToString("x64").PadLeft(64, '0'));
    }

    // --- Construction ---

    [Fact]
    public void Constructor_ValidArgs_InitialisesCorrectly()
    {
        var b = MakeBlock();
        Assert.Equal(BigInteger.One, b.BlockNumber);
        Assert.Equal("prev_hash", b.PreviousHash);
        Assert.Equal("producer_addr", b.ProducerAddress);
        Assert.Equal(0, b.TransactionBlockCount);
        Assert.False(b.IsSealed);
        Assert.False(b.IsValid);
    }

    [Fact]
    public void Constructor_NegativeBlockNumber_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new StateBlock(new BigInteger(-1), "prev", "prod"));

    [Fact]
    public void Constructor_EmptyPreviousHash_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new StateBlock(BigInteger.Zero, "", "prod"));

    [Fact]
    public void Constructor_EmptyProducerAddress_Throws()
        => Assert.Throws<ArgumentException>(() =>
            new StateBlock(BigInteger.Zero, "prev", ""));

    // --- AddTransactionBlockHash ---

    [Fact]
    public void AddTransactionBlockHash_ValidHash_IncreasesCount()
    {
        var b = MakeBlock();
        b.AddTransactionBlockHash(MakeHash('a'));
        Assert.Equal(1, b.TransactionBlockCount);
    }

    [Fact]
    public void AddTransactionBlockHash_EmptyHash_Throws()
        => Assert.Throws<ArgumentException>(() => MakeBlock().AddTransactionBlockHash(""));

    [Fact]
    public void AddTransactionBlockHash_Duplicate_Throws()
    {
        var b = MakeBlock();
        b.AddTransactionBlockHash(MakeHash('a'));
        Assert.Throws<InvalidOperationException>(() => b.AddTransactionBlockHash(MakeHash('a')));
    }

    [Fact]
    public void AddTransactionBlockHash_AfterSealed_Throws()
    {
        var b = MakeBlock();
        FillToCapacity(b);
        b.Seal(MakeHash('x'), MakeHash('y'), "global_state", Atomos.Zero);
        Assert.Throws<InvalidOperationException>(() => b.AddTransactionBlockHash(MakeHash('z')));
    }

    [Fact]
    public void AddTransactionBlockHash_BeyondCapacity_Throws()
    {
        var b = MakeBlock();
        FillToCapacity(b);
        Assert.Throws<InvalidOperationException>(() =>
            b.AddTransactionBlockHash(MakeHash('z')));
    }

    // --- Seal ---

    [Fact]
    public void Seal_AtCapacity_SetsIsSealed()
    {
        var b = MakeBlock();
        FillToCapacity(b);
        b.Seal(MakeHash('x'), MakeHash('y'), "global_state", new Atomos(999));
        Assert.True(b.IsSealed);
        Assert.True(b.IsValid);
        Assert.Equal("global_state", b.GlobalStateRoot);
        Assert.Equal(new Atomos(999), b.TotalFeesCollected);
    }

    [Fact]
    public void Seal_BelowCapacity_Throws()
    {
        var b = MakeBlock();
        b.AddTransactionBlockHash(MakeHash('a'));
        Assert.Throws<InvalidOperationException>(() =>
            b.Seal(MakeHash('x'), MakeHash('y'), "global_state", Atomos.Zero));
    }

    [Fact]
    public void Seal_EmptyGlobalStateRoot_Throws()
    {
        var b = MakeBlock();
        FillToCapacity(b);
        Assert.Throws<ArgumentException>(() =>
            b.Seal(MakeHash('x'), MakeHash('y'), "", Atomos.Zero));
    }

    [Fact]
    public void Seal_EmptyHash_Throws()
    {
        var b = MakeBlock();
        FillToCapacity(b);
        Assert.Throws<ArgumentException>(() =>
            b.Seal("", MakeHash('y'), "global_state", Atomos.Zero));
    }

    [Fact]
    public void Seal_EmptyMerkleRoot_Throws()
    {
        var b = MakeBlock();
        FillToCapacity(b);
        Assert.Throws<ArgumentException>(() =>
            b.Seal(MakeHash('x'), "", "global_state", Atomos.Zero));
    }

    [Fact]
    public void Seal_Twice_Throws()
    {
        var b = MakeBlock();
        FillToCapacity(b);
        b.Seal(MakeHash('x'), MakeHash('y'), "global_state", Atomos.Zero);
        Assert.Throws<InvalidOperationException>(() =>
            b.Seal(MakeHash('p'), MakeHash('q'), "global_state2", Atomos.Zero));
    }

    // --- IsValid ---

    [Fact]
    public void IsValid_BeforeSeal_ReturnsFalse()
    {
        var b = MakeBlock();
        FillToCapacity(b);
        Assert.False(b.IsValid);
    }

    [Fact]
    public void IsValid_AfterSeal_ReturnsTrue()
    {
        var b = MakeBlock();
        FillToCapacity(b);
        b.Seal(MakeHash('x'), MakeHash('y'), "global_state", Atomos.Zero);
        Assert.True(b.IsValid);
    }

    [Fact]
    public void IsValid_EmptyBlock_ReturnsFalse()
        => Assert.False(MakeBlock().IsValid);
}
