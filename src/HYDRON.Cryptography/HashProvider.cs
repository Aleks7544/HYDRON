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

            using IncrementalHashWriter writer = new();
            foreach (string hash in sorted)
                writer.WriteString(hash);

            return writer.FinalizeHex();
        }

        private sealed class IncrementalHashWriter : IDisposable
        {
            private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            private readonly byte[] _buf = new byte[8];

            public void WriteString(string value)
            {
                byte[] encoded = Encoding.UTF8.GetBytes(value);
                WriteInt32(encoded.Length);
                _hash.AppendData(encoded);
            }

            public void WriteAtomos(Atomos value) => WriteString(value.ToString());

            public void WriteBigInteger(BigInteger value) => WriteString(value.ToString());

            public void WriteInt32(int value)
            {
                System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(_buf, value);
                _hash.AppendData(_buf.AsSpan(0, 4));
            }

            public void WriteInt64(long value)
            {
                System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(_buf, value);
                _hash.AppendData(_buf.AsSpan(0, 8));
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