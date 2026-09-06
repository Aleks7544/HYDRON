using System.Numerics;
using HYDRON.Models;

namespace HYDRON.Tests;

public class MempoolTests
{
    private static Transaction MakePendingTx(
        string sender = "alice",
        string receiver = "bob",
        string hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        BigInteger? nonce = null,
        Atomos? fee = null,
        Priority priority = Priority.Low)
    {
        var tx = new Transaction(sender, receiver,
            new Atomos(1000), fee ?? new Atomos(100),
            nonce ?? BigInteger.One, "sig", false);
        tx.SetHash(hash);
        tx.ChangePriority(priority); // must be called before PendingValidation
        tx.UpdateStatus(TransactionStatus.PendingValidation);
        return tx;
    }

    // --- TryEnqueue ---

    [Fact]
    public void TryEnqueue_ValidTx_ReturnsTrueAndIncreasesCount()
    {
        var pool = new Mempool();
        var tx = MakePendingTx();
        Assert.True(pool.TryEnqueue(tx));
        Assert.Equal(1, pool.Count);
    }

    [Fact]
    public void TryEnqueue_NullTx_Throws()
        => Assert.Throws<ArgumentNullException>(() => new Mempool().TryEnqueue(null!));

    [Fact]
    public void TryEnqueue_NoHash_Throws()
    {
        var pool = new Mempool();
        var tx = new Transaction("alice", "bob", new Atomos(1000), new Atomos(100),
            BigInteger.One, "sig", false);
        tx.UpdateStatus(TransactionStatus.PendingValidation);
        Assert.Throws<InvalidOperationException>(() => pool.TryEnqueue(tx));
    }

    [Fact]
    public void TryEnqueue_WrongStatus_Throws()
    {
        var pool = new Mempool();
        var tx = new Transaction("alice", "bob", new Atomos(1000), new Atomos(100),
            BigInteger.One, "sig", false);
        tx.SetHash(new string('a', 64));
        Assert.Throws<InvalidOperationException>(() => pool.TryEnqueue(tx));
    }

    [Fact]
    public void TryEnqueue_Duplicate_ReturnsFalse()
    {
        var pool = new Mempool();
        var tx = MakePendingTx();
        pool.TryEnqueue(tx);
        Assert.False(pool.TryEnqueue(tx));
        Assert.Equal(1, pool.Count);
    }

    // --- TryRemove ---

    [Fact]
    public void TryRemove_ExistingHash_ReturnsTrueAndDecreasesCount()
    {
        var pool = new Mempool();
        var tx = MakePendingTx();
        pool.TryEnqueue(tx);
        Assert.True(pool.TryRemove(tx.Hash));
        Assert.Equal(0, pool.Count);
    }

    [Fact]
    public void TryRemove_NonExistingHash_ReturnsFalse()
        => Assert.False(new Mempool().TryRemove(new string('a', 64)));

    [Fact]
    public void TryRemove_EmptyHash_Throws()
        => Assert.Throws<ArgumentException>(() => new Mempool().TryRemove(""));

    [Fact]
    public void TryRemove_LastTxForSender_RemovesSenderEntry()
    {
        var pool = new Mempool();
        var tx = MakePendingTx();
        pool.TryEnqueue(tx);
        pool.TryRemove(tx.Hash);
        Assert.Empty(pool.GetHashesBySender("alice"));
    }

    // --- Contains ---

    [Fact]
    public void Contains_AfterEnqueue_ReturnsTrue()
    {
        var pool = new Mempool();
        var tx = MakePendingTx();
        pool.TryEnqueue(tx);
        Assert.True(pool.Contains(tx.Hash));
    }

    [Fact]
    public void Contains_AfterRemove_ReturnsFalse()
    {
        var pool = new Mempool();
        var tx = MakePendingTx();
        pool.TryEnqueue(tx);
        pool.TryRemove(tx.Hash);
        Assert.False(pool.Contains(tx.Hash));
    }

    [Fact]
    public void Contains_EmptyHash_ReturnsFalse()
        => Assert.False(new Mempool().Contains(""));

    // --- TryGet ---

    [Fact]
    public void TryGet_ExistingHash_ReturnsTrueAndTx()
    {
        var pool = new Mempool();
        var tx = MakePendingTx();
        pool.TryEnqueue(tx);
        Assert.True(pool.TryGet(tx.Hash, out var result));
        Assert.Same(tx, result);
    }

    [Fact]
    public void TryGet_NonExistingHash_ReturnsFalse()
        => Assert.False(new Mempool().TryGet(new string('a', 64), out _));

    // --- PeekForBlock ---

    [Fact]
    public void PeekForBlock_ZeroMaxCount_Throws()
        => Assert.Throws<ArgumentException>(() => new Mempool().PeekForBlock(0));

    [Fact]
    public void PeekForBlock_NegativeMaxCount_Throws()
        => Assert.Throws<ArgumentException>(() => new Mempool().PeekForBlock(-1));

    [Fact]
    public void PeekForBlock_RespectsMaxCount()
    {
        var pool = new Mempool();
        pool.TryEnqueue(MakePendingTx(hash: new string('a', 64)));
        pool.TryEnqueue(MakePendingTx(hash: new string('b', 64)));
        pool.TryEnqueue(MakePendingTx(hash: new string('c', 64)));
        Assert.Single(pool.PeekForBlock(1));
    }

    [Fact]
    public void PeekForBlock_OrdersByPriorityThenFeeThenTime()
    {
        var pool = new Mempool();
        pool.TryEnqueue(MakePendingTx(hash: new string('a', 64), fee: new Atomos(10),  priority: Priority.Low));
        pool.TryEnqueue(MakePendingTx(hash: new string('b', 64), fee: new Atomos(500), priority: Priority.High));
        pool.TryEnqueue(MakePendingTx(hash: new string('c', 64), fee: new Atomos(200), priority: Priority.Medium));

        var result = pool.PeekForBlock(3);
        Assert.Equal(new string('b', 64), result[0].Hash);
        Assert.Equal(new string('c', 64), result[1].Hash);
        Assert.Equal(new string('a', 64), result[2].Hash);
    }

    [Fact]
    public void PeekForBlock_DoesNotRemoveTxFromPool()
    {
        var pool = new Mempool();
        pool.TryEnqueue(MakePendingTx());
        pool.PeekForBlock(10);
        Assert.Equal(1, pool.Count);
    }

    // --- GetHashesBySender ---

    [Fact]
    public void GetHashesBySender_EmptyAddress_Throws()
        => Assert.Throws<ArgumentException>(() => new Mempool().GetHashesBySender(""));

    [Fact]
    public void GetHashesBySender_UnknownSender_ReturnsEmpty()
        => Assert.Empty(new Mempool().GetHashesBySender("unknown"));

    [Fact]
    public void GetHashesBySender_KnownSender_ReturnsHashes()
    {
        var pool = new Mempool();
        pool.TryEnqueue(MakePendingTx(hash: new string('a', 64)));
        pool.TryEnqueue(MakePendingTx(hash: new string('b', 64)));
        var hashes = pool.GetHashesBySender("alice");
        Assert.Equal(2, hashes.Count);
    }

    // --- EvictStaleBySender ---

    [Fact]
    public void EvictStaleBySender_EmptyAddress_Throws()
        => Assert.Throws<ArgumentException>(() => new Mempool().EvictStaleBySender("", BigInteger.One));

    [Fact]
    public void EvictStaleBySender_UnknownSender_ReturnsZero()
        => Assert.Equal(0, new Mempool().EvictStaleBySender("nobody", BigInteger.One));

    [Fact]
    public void EvictStaleBySender_EvictsOnlyStaleNonces()
    {
        var pool = new Mempool();
        pool.TryEnqueue(MakePendingTx(hash: new string('a', 64), nonce: new BigInteger(1)));
        pool.TryEnqueue(MakePendingTx(hash: new string('b', 64), nonce: new BigInteger(2)));
        pool.TryEnqueue(MakePendingTx(hash: new string('c', 64), nonce: new BigInteger(3)));

        int evicted = pool.EvictStaleBySender("alice", new BigInteger(3));
        Assert.Equal(2, evicted);
        Assert.Equal(1, pool.Count);
    }

    [Fact]
    public void EvictStaleBySender_NothingStale_ReturnsZero()
    {
        var pool = new Mempool();
        pool.TryEnqueue(MakePendingTx(hash: new string('a', 64), nonce: new BigInteger(5)));
        Assert.Equal(0, pool.EvictStaleBySender("alice", new BigInteger(3)));
    }

    // --- Clear ---

    [Fact]
    public void Clear_RemovesAllTransactions()
    {
        var pool = new Mempool();
        pool.TryEnqueue(MakePendingTx(hash: new string('a', 64)));
        pool.TryEnqueue(MakePendingTx(hash: new string('b', 64)));
        pool.Clear();
        Assert.Equal(0, pool.Count);
        Assert.Empty(pool.GetHashesBySender("alice"));
    }
}
