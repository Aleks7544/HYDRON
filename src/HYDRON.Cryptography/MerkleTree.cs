using System.Security.Cryptography;

namespace HYDRON.Cryptography
{
    public static class MerkleTree
    {
        public static readonly string EmptyRoot =
            Convert.ToHexStringLower(SHA256.HashData([]));

        public static string ComputeRoot(IReadOnlyList<string> transactionHashes)
        {
            ArgumentNullException.ThrowIfNull(transactionHashes);

            if (transactionHashes.Count == 0)
                return EmptyRoot;

            if (transactionHashes.Any(h => h.Length != CryptoConstants.Sha256HexLength))
                throw new ArgumentException(
                    $"All transaction hashes must be {CryptoConstants.Sha256HexLength}-character lowercase hex strings.",
                    nameof(transactionHashes));

            List<byte[]> level = transactionHashes
                .Select(Convert.FromHexString)
                .ToList();

            while (level.Count > 1)
            {
                if (level.Count % 2 != 0)
                    level.Add(level[^1]);

                List<byte[]> nextLevel = new(level.Count / 2);
                for (int i = 0; i < level.Count; i += 2)
                    nextLevel.Add(HashPair(level[i], level[i + 1]));

                level = nextLevel;
            }

            return Convert.ToHexStringLower(level[0]);
        }

        private static byte[] HashPair(byte[] left, byte[] right)
        {
            Span<byte> combined = stackalloc byte[64];
            left.CopyTo(combined);
            right.CopyTo(combined[32..]);
            return SHA256.HashData(combined);
        }
    }
}