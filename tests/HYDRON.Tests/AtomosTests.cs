using System.Numerics;
using HYDRON.Models;

namespace HYDRON.Tests;

public class AtomosTests
{
    [Fact]
    public void Constructor_ZeroIsValid()
        => Assert.Equal(Atomos.Zero, new Atomos(BigInteger.Zero));

    [Fact]
    public void Constructor_NegativeThrows()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Atomos(new BigInteger(-1)));

    [Fact]
    public void Equality_SameValue_IsEqual()
    {
        var a = new Atomos(42);
        var b = new Atomos(42);
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void Equality_DifferentValue_IsNotEqual()
    {
        var a = new Atomos(1);
        var b = new Atomos(2);
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void Comparison_Operators_WorkCorrectly()
    {
        var small = new Atomos(1);
        var large = new Atomos(100);
        Assert.True(small < large);
        Assert.True(small <= large);
        Assert.True(large > small);
        Assert.True(large >= small);
        Assert.True(small <= small);
        Assert.True(small >= small);
    }

    [Fact]
    public void Addition_ProducesCorrectSum()
        => Assert.Equal(new Atomos(300), new Atomos(100) + new Atomos(200));

    [Fact]
    public void Subtraction_ValidAmount_ProducesCorrectResult()
        => Assert.Equal(new Atomos(50), new Atomos(150) - new Atomos(100));

    [Fact]
    public void Subtraction_WouldGoNegative_Throws()
        => Assert.Throws<InvalidOperationException>(() => new Atomos(10) - new Atomos(20));

    [Fact]
    public void Multiplication_ByPositiveInt_ProducesCorrectResult()
        => Assert.Equal(new Atomos(500), new Atomos(100) * 5);

    [Fact]
    public void Multiplication_ByNegative_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Atomos(100) * new BigInteger(-1));

    [Fact]
    public void Division_ByPositiveInt_ProducesCorrectResult()
        => Assert.Equal(new Atomos(25), new Atomos(100) / 4);

    [Fact]
    public void Division_ByZero_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Atomos(100) / 0);

    [Fact]
    public void Modulo_ProducesRemainder()
        => Assert.Equal(new Atomos(1), new Atomos(101) % new Atomos(100));

    [Fact]
    public void Increment_IncreasesValueByOne()
    {
        var a = new Atomos(9);
        a++;
        Assert.Equal(new Atomos(10), a);
    }

    [Fact]
    public void Decrement_DecreasesValueByOne()
    {
        var a = new Atomos(10);
        a--;
        Assert.Equal(new Atomos(9), a);
    }

    [Fact]
    public void Decrement_AtZero_Throws()
    {
        var a = Atomos.Zero;
        Assert.Throws<InvalidOperationException>(() => a--);
    }

    [Fact]
    public void Scale_HalvesValue()
        => Assert.Equal(new Atomos(50), new Atomos(100).Scale(1, 2));

    [Fact]
    public void Scale_NegativeNumerator_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Atomos(100).Scale(-1, 2));

    [Fact]
    public void Scale_ZeroDenominator_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Atomos(100).Scale(1, 0));

    [Theory]
    [InlineData(Denominations.Hya, 100L)]
    [InlineData(Denominations.Hyb, 10_000L)]
    [InlineData(Denominations.Hyg, 100_000_000L)]
    public void FromDenomination_One_EqualsExpectedAtomosCount(Denominations denom, long expectedAtomosCount)
        => Assert.Equal(new Atomos(new BigInteger(expectedAtomosCount)), Atomos.FromDenomination(1, denom));

    [Fact]
    public void FromDenomination_Negative_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Atomos.FromDenomination(-1, Denominations.Hya));

    [Fact]
    public void ToDenomination_RoundTrip_IsCorrect()
    {
        var a = Atomos.FromDenomination(3.5, Denominations.Hya);
        Assert.Equal(3.5, a.ToDenomination(Denominations.Hya));
    }

    [Fact]
    public void RemainderAfterDenomination_IsCorrect()
    {
        var a = new Atomos(250);
        Assert.Equal(new Atomos(50), a.RemainderAfterDenomination(Denominations.Hya));
    }

    [Fact]
    public void ExplicitCast_ToBigInteger_Works()
        => Assert.Equal(new BigInteger(42), (BigInteger)new Atomos(42));

    [Fact]
    public void ExplicitCast_ToDouble_Works()
        => Assert.Equal(42.0, (double)new Atomos(42));

    [Fact]
    public void ToString_ReturnsDecimalString()
        => Assert.Equal("12345", new Atomos(12345).ToString());
}
