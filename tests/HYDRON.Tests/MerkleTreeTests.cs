using HYDRON.Cryptography;

namespace HYDRON.Tests;

public class MerkleTreeTests
{
    // a valid 64-char lowercase hex SHA-256 hash
    private static string FakeHash(char fill) => new(fill, 64);

    private static readonly string H1 = FakeHash('a');
    private static readonly string H2 = FakeHash('b');
    private static readonly string H3 = FakeHash('c');
    private static readonly string H4 = FakeHash('d');

    // --- EmptyRoot ---

    [Fact]
    public void EmptyRoot_Is64CharHex()
    {
        Assert.Equal(64, MerkleTree.EmptyRoot.Length);
        Assert.Matches("^[0-9a-f]{64}$", MerkleTree.EmptyRoot);
    }

    // --- Empty list ---

    [Fact]
    public void ComputeRoot_EmptyList_ReturnsEmptyRoot()
        => Assert.Equal(MerkleTree.EmptyRoot, MerkleTree.ComputeRoot([]));

    // --- Single hash ---

    [Fact]
    public void ComputeRoot_SingleHash_ReturnsItself()
        => Assert.Equal(H1, MerkleTree.ComputeRoot([H1]));

    // --- Two hashes ---

    [Fact]
    public void ComputeRoot_TwoHashes_ReturnsCombined()
    {
        string root = MerkleTree.ComputeRoot([H1, H2]);
        Assert.Equal(64, root.Length);
        Assert.NotEqual(H1, root);
        Assert.NotEqual(H2, root);
    }

    [Fact]
    public void ComputeRoot_TwoHashes_OrderMatters()
    {
        string r1 = MerkleTree.ComputeRoot([H1, H2]);
        string r2 = MerkleTree.ComputeRoot([H2, H1]);
        Assert.NotEqual(r1, r2);
    }

    // --- Odd number of hashes (duplication of last) ---

    [Fact]
    public void ComputeRoot_ThreeHashes_IsDeterministic()
    {
        string r1 = MerkleTree.ComputeRoot([H1, H2, H3]);
        string r2 = MerkleTree.ComputeRoot([H1, H2, H3]);
        Assert.Equal(r1, r2);
    }

    [Fact]
    public void ComputeRoot_ThreeHashes_DifferentFromTwo()
    {
        string r2 = MerkleTree.ComputeRoot([H1, H2]);
        string r3 = MerkleTree.ComputeRoot([H1, H2, H3]);
        Assert.NotEqual(r2, r3);
    }

    // --- Four hashes ---

    [Fact]
    public void ComputeRoot_FourHashes_IsDeterministic()
    {
        string r1 = MerkleTree.ComputeRoot([H1, H2, H3, H4]);
        string r2 = MerkleTree.ComputeRoot([H1, H2, H3, H4]);
        Assert.Equal(r1, r2);
    }

    // --- Invalid hash lengths ---

    [Fact]
    public void ComputeRoot_WrongLengthHash_Throws()
        => Assert.Throws<ArgumentException>(() => MerkleTree.ComputeRoot(["tooshort"]));

    [Fact]
    public void ComputeRoot_MixedValidInvalidLengths_Throws()
        => Assert.Throws<ArgumentException>(() => MerkleTree.ComputeRoot([H1, "bad"]));

    // --- Null ---

    [Fact]
    public void ComputeRoot_NullArgument_Throws()
        => Assert.Throws<ArgumentNullException>(() => MerkleTree.ComputeRoot(null!));

    // --- Returns 64-char hex ---

    [Fact]
    public void ComputeRoot_AlwaysReturns64CharHex()
    {
        foreach (var hashes in new[] {
            new[] { H1 },
            new[] { H1, H2 },
            new[] { H1, H2, H3 },
            new[] { H1, H2, H3, H4 }
        })
        {
            string root = MerkleTree.ComputeRoot(hashes);
            Assert.Equal(64, root.Length);
            Assert.Matches("^[0-9a-f]{64}$", root);
        }
    }
}
