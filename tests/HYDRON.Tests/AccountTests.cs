using System.Numerics;
using HYDRON.Models;
using Xunit;

namespace HYDRON.Tests;

public class AccountTests
{
    private static Account MakeAccount(string address = "addr1") =>
        new(address, "pubkey1", "stealth1");

    [Fact]
    public void Constructor_ValidArgs_InitialisesCorrectly()
    {
        Account a = MakeAccount("addr1");
        Assert.Equal("addr1", a.Address);
        Assert.Equal(Atomos.Zero, a.Balance);
        Assert.Equal(BigInteger.Zero, a.Nonce);
        Assert.Null(a.Handle);
    }

    [Theory]
    [InlineData("", "pub", "stealth")]
    [InlineData("addr", "", "stealth")]
    [InlineData("addr", "pub", "")]
    public void Constructor_EmptyArgs_Throws(string address, string pub, string stealth)
        => Assert.Throws<ArgumentException>(() => new Account(address, pub, stealth));

    [Fact]
    public void AddBalance_IncreasesBalance()
    {
        Account a = MakeAccount();
        a.AddBalance(new Atomos(500));
        Assert.Equal(new Atomos(500), a.Balance);
    }

    [Fact]
    public void TryDeductBalance_SufficientFunds_ReturnsTrueAndDeducts()
    {
        Account a = MakeAccount();
        a.AddBalance(new Atomos(1000));
        bool result = a.TryDeductBalance(new Atomos(400));
        Assert.True(result);
        Assert.Equal(new Atomos(600), a.Balance);
    }

    [Fact]
    public void TryDeductBalance_InsufficientFunds_ReturnsFalseAndKeepsBalance()
    {
        Account a = MakeAccount();
        a.AddBalance(new Atomos(100));
        bool result = a.TryDeductBalance(new Atomos(200));
        Assert.False(result);
        Assert.Equal(new Atomos(100), a.Balance);
    }

    [Fact]
    public void IncrementNonce_IncreasesNonceByOne()
    {
        Account a = MakeAccount();
        a.IncrementNonce();
        Assert.Equal(BigInteger.One, a.Nonce);
        a.IncrementNonce();
        Assert.Equal(new BigInteger(2), a.Nonce);
    }

    [Fact]
    public void UpdateHandle_ValidHandle_SetsHandle()
    {
        Account a = MakeAccount();
        a.UpdateHandle("hydron_user");
        Assert.Equal("hydron_user", a.Handle);
    }

    [Fact]
    public void UpdateHandle_Null_ClearsHandle()
    {
        Account a = MakeAccount();
        a.UpdateHandle("hydron_user");
        a.UpdateHandle(null);
        Assert.Null(a.Handle);
    }

    [Fact]
    public void UpdateHandle_WhitespaceOnly_Throws()
        => Assert.Throws<ArgumentException>(() => MakeAccount().UpdateHandle("   "));

    [Fact]
    public void UpdateHandle_ExceedsMaxBytes_Throws()
    {
        string tooLong = new('a', 1001);
        Assert.Throws<ArgumentException>(() => MakeAccount().UpdateHandle(tooLong));
    }

    [Fact]
    public void StateHash_IsDeterministic()
    {
        Account a = MakeAccount();
        Assert.Equal(a.StateHash, a.StateHash);
    }

    [Fact]
    public void StateHash_ChangesAfterBalanceMutation()
    {
        Account a = MakeAccount();
        string before = a.StateHash;
        a.AddBalance(new Atomos(1));
        Assert.NotEqual(before, a.StateHash);
    }

    [Fact]
    public void StateHash_ChangesAfterNonceIncrement()
    {
        Account a = MakeAccount();
        string before = a.StateHash;
        a.IncrementNonce();
        Assert.NotEqual(before, a.StateHash);
    }

    [Fact]
    public async Task AddBalance_ConcurrentCalls_ProducesCorrectTotal()
    {
        Account a = MakeAccount();
        IEnumerable<Task> tasks = Enumerable.Range(0, 1000)
            .Select(_ => Task.Run(() => a.AddBalance(new Atomos(1))));
        await Task.WhenAll(tasks);
        Assert.Equal(new Atomos(1000), a.Balance);
    }
}
