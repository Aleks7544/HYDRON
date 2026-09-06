using HYDRON.Models;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace HYDRON.Cryptography
{
    public static class HashProvider
    {
        public static string HashTransaction(Transaction tx)
        {
            ArgumentNullException.ThrowIfNull(tx);

            using IncrementalHashWriter writer = new();
            writer.WriteString(tx.Sender);
            writer.WriteString(tx.Receiver);
            writer.WriteAtomos(tx.Amount);
            writer.WriteAtomos(tx.Fee);
            writer.WriteBigInteger(tx.Nonce);
            writer.WriteString(tx.SenderSignature);
            writer.WriteBool(tx.RequiresReceiverConfirmation);
            writer.WriteInt32((int)tx.PrivacyMode);
            writer.WriteString(tx.EphemeralPublicKey ?? string.Empty);
            writer.WriteInt64(tx.InitiatedAt.ToUnixTimeMilliseconds());
            return writer.FinalizeHex();
        }

        public static string HashTransactionBlockHeader(
            BigInteger blockNumber,
            string previousHash,
            string producerAddress,
            DateTimeOffset timestamp,
            string merkleRoot,
            string stateRoot,
            Atomos electricityPriceAtomosPerEv)
        {
            using IncrementalHashWriter writer = new();
            writer.WriteBigInteger(blockNumber);
            writer.WriteString(previousHash);
            writer.WriteString(producerAddress);
            writer.WriteInt64(timestamp.ToUnixTimeMilliseconds());
            writer.WriteString(merkleRoot);
            writer.WriteString(stateRoot);
            writer.WriteAtomos(electricityPriceAtomosPerEv);
            return writer.FinalizeHex();
        }

        public static string HashStateBlockHeader(
            BigInteger blockNumber,
            string previousHash,
            string producerAddress,
            DateTimeOffset timestamp,
            string merkleRoot,
            string globalStateRoot,
            Atomos totalFeesCollected)
        {
            using IncrementalHashWriter writer = new();
            writer.WriteBigInteger(blockNumber);
            writer.WriteString(previousHash);
            writer.WriteString(producerAddress);
            writer.WriteInt64(timestamp.ToUnixTimeMilliseconds());
            writer.WriteString(merkleRoot);
            writer.WriteString(globalStateRoot);
            writer.WriteAtomos(totalFeesCollected);
            return writer.FinalizeHex();
        }

        public static string ComputeStateRoot(IEnumerable<string> accountStateHashes)
        {
            ArgumentNullException.ThrowIfNull(accountStateHashes);

            string[] sorted = accountStateHashes
                .OrderBy(h => h, StringComparer.Ordinal)
                .ToArray();

            if (sorted.Length == 0) 
                throw new ArgumentException("Cannot compute state root from an empty account set.", nameof(accountStateHashes));

            using IncrementalHashWriter writer = new();
            foreach (string hash in sorted)
                writer.WriteString(hash);

            return writer.FinalizeHex();
        }

        private sealed class IncrementalHashWriter : IDisposable
        {
            private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            public void WriteString(string value)
            {
                int maxLen = Encoding.UTF8.GetMaxByteCount(value.Length);
                byte[] rented = System.Buffers.ArrayPool<byte>.Shared.Rent(maxLen);
                try
                {
                    int written = Encoding.UTF8.GetBytes(value, rented);
                    WriteInt32(written);
                    _hash.AppendData(rented.AsSpan(0, written));
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(rented);
                }
            }

            public void WriteAtomos(Atomos value) => WriteString(value.ToString());

            public void WriteBigInteger(BigInteger value) => WriteString(value.ToString());

            public void WriteInt32(int value)
            {
                Span<byte> buf = stackalloc byte[4];
                System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(buf, value);
                _hash.AppendData(buf);
            }

            public void WriteInt64(long value)
            {
                Span<byte> buf = stackalloc byte[8];
                System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(buf, value);
                _hash.AppendData(buf);
            }

            public void WriteBool(bool value) =>
                _hash.AppendData([(byte)(value ? 0x01 : 0x00)]);

            public string FinalizeHex()
            {
                Span<byte> output = stackalloc byte[CryptoConstants.Sha256Bytes];
                _hash.GetHashAndReset(output);
                return Convert.ToHexStringLower(output);
            }

            public void Dispose() => _hash.Dispose();
        }
    }
}