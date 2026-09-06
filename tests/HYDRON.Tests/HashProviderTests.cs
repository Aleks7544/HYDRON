using System.Numerics;
using HYDRON.Cryptography;
using HYDRON.Models;
using Xunit;

namespace HYDRON.Tests;

public class HashProviderTests
{
    private static Transaction MakeTx() =>
        new("alice", "bob", new Atomos(1000), new Atomos(100),
            BigInteger.One, "sig_sender", false);

    [Fact]
    public void HashTransaction_ReturnsSha256HexString()
    {
        string hash = HashProvider.HashTransaction(MakeTx());
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void HashTransaction_IsDeterministic()
    {
        Transaction tx = MakeTx();
        Assert.Equal(HashProvider.HashTransaction(tx), HashProvider.HashTransaction(tx));
    }

    [Fact]
    public void HashTransaction_NullArgument_Throws()
        => Assert.Throws<ArgumentNullException>(() => HashProvider.HashTransaction(null!));

    [Fact]
    public void HashTransaction_DifferentTx_ProducesDifferentHash()
    {
        Transaction tx1 = new Transaction("alice", "bob", new Atomos(1000), new Atomos(100), BigInteger.One, "sig", false);
        Transaction tx2 = new Transaction("alice", "bob", new Atomos(2000), new Atomos(100), BigInteger.One, "sig", false);
        Assert.NotEqual(HashProvider.HashTransaction(tx1), HashProvider.HashTransaction(tx2));
    }

    [Fact]
    public void HashTransactionBlockHeader_ReturnsSha256HexString()
    {
        string hash = HashProvider.HashTransactionBlockHeader(
            BigInteger.One, "prev_hash", "producer",
            DateTimeOffset.UtcNow, "merkle", "state", new Atomos(42));
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void HashTransactionBlockHeader_IsDeterministic()
    {
        DateTimeOffset ts = DateTimeOffset.UtcNow;
        string h1 = HashProvider.HashTransactionBlockHeader(
            BigInteger.One, "prev", "prod", ts, "merkle", "state", new Atomos(1));
        string h2 = HashProvider.HashTransactionBlockHeader(
            BigInteger.One, "prev", "prod", ts, "merkle", "state", new Atomos(1));
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void HashTransactionBlockHeader_ChangeInAnyField_ProducesDifferentHash()
    {
        DateTimeOffset ts = DateTimeOffset.UtcNow;
        string base_hash = HashProvider.HashTransactionBlockHeader(
            BigInteger.One, "prev", "prod", ts, "merkle", "state", new Atomos(1));
        string diff_block = HashProvider.HashTransactionBlockHeader(
            new BigInteger(2), "prev", "prod", ts, "merkle", "state", new Atomos(1));
        string diff_prev = HashProvider.HashTransactionBlockHeader(
            BigInteger.One, "other_prev", "prod", ts, "merkle", "state", new Atomos(1));
        Assert.NotEqual(base_hash, diff_block);
        Assert.NotEqual(base_hash, diff_prev);
    }

    [Fact]
    public void HashStateBlockHeader_ReturnsSha256HexString()
    {
        string hash = HashProvider.HashStateBlockHeader(
            BigInteger.One, "prev", "prod",
            DateTimeOffset.UtcNow, "merkle", "global_state", new Atomos(999));
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void ComputeStateRoot_ReturnsSha256HexString()
    {
        string root = HashProvider.ComputeStateRoot(["aaa", "bbb", "ccc"]);
        Assert.Equal(64, root.Length);
        Assert.Matches("^[0-9a-f]{64}$", root);
    }

    [Fact]
    public void ComputeStateRoot_IsOrderIndependent()
    {
        string r1 = HashProvider.ComputeStateRoot(["aaa", "bbb", "ccc"]);
        string r2 = HashProvider.ComputeStateRoot(["ccc", "aaa", "bbb"]);
        Assert.Equal(r1, r2);
    }

    [Fact]
    public void ComputeStateRoot_EmptySet_Throws()
        => Assert.Throws<ArgumentException>(() => HashProvider.ComputeStateRoot([]));

    [Fact]
    public void ComputeStateRoot_NullArgument_Throws()
        => Assert.Throws<ArgumentNullException>(() => HashProvider.ComputeStateRoot(null!));

    [Fact]
    public void ComputeStateRoot_DifferentHashes_ProduceDifferentRoot()
    {
        string r1 = HashProvider.ComputeStateRoot(["aaa"]);
        string r2 = HashProvider.ComputeStateRoot(["bbb"]);
        Assert.NotEqual(r1, r2);
    }
}
