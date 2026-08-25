using System.Numerics;

namespace HYDRON.Database
{
    internal static class KeyScheme
    {
        public static string Account(string address) => $"acc:{address}";
        public static readonly string AccountPrefix = "acc:";

        public static string Validator(string address) => $"val:{address}";
        public static readonly string ValidatorPrefix = "val:";

        public static string Transaction(string hash) => $"tx:{hash}";
        public static string TransactionBySender(string address, string hash) => $"txs:{address}:{hash}";
        public static readonly string TransactionPrefix = "tx:";
        public static string TransactionBySenderPrefix(string address) => $"txs:{address}:";

        public static string TransactionBlock(BigInteger blockNumber) => $"tb:{ToBlockKey(blockNumber)}";
        public static string TransactionBlockByHash(string hash) => $"tbh:{hash}";
        public static readonly string TransactionBlockPrefix = "tb:";

        public static string StateBlock(BigInteger blockNumber) => $"sb:{ToBlockKey(blockNumber)}";
        public static string StateBlockByHash(string hash) => $"sbh:{hash}";
        public static readonly string StateBlockPrefix = "sb:";

        public static readonly string LatestTransactionBlockNumber = "meta:latest_tb";
        public static readonly string LatestStateBlockNumber = "meta:latest_sb";

        private const int BlockKeyBytes = 32;

        private static string ToBlockKey(BigInteger blockNumber)
        {
            if (blockNumber < BigInteger.Zero)
                throw new ArgumentException("Block number cannot be negative.", nameof(blockNumber));

            Span<byte> beBytes = stackalloc byte[BlockKeyBytes];
            beBytes.Clear();

            Span<byte> leBytes = stackalloc byte[BlockKeyBytes];
            if (!blockNumber.TryWriteBytes(leBytes, out int bytesWritten, isUnsigned: true, isBigEndian: false))
                throw new OverflowException($"Block number {blockNumber} exceeds {BlockKeyBytes * 8}-bit range.");

            for (int i = 0; i < bytesWritten; i++)
                beBytes[BlockKeyBytes - 1 - i] = leBytes[i];

            return Convert.ToHexStringLower(beBytes);
        }
    }
}