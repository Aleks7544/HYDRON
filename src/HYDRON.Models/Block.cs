// src/HYDRON.Models/Block.cs
using System.Numerics;

namespace HYDRON.Models
{
    public abstract class Block
    {
        public const int Capacity = 100;

        public BigInteger BlockNumber { get; protected init; }
        public string Hash { get; protected set; }
        public string PreviousHash { get; protected init; }
        public DateTimeOffset Timestamp { get; protected init; }

        public string ProducerAddress { get; protected init; }

        public string MerkleRoot { get; protected set; }

        public bool IsSealed { get; private set; }
        protected readonly Lock WriteLock = new();

        protected Block(
            BigInteger blockNumber,
            string previousHash,
            string producerAddress)
        {
            if (blockNumber < BigInteger.Zero)
                throw new ArgumentException("Block number cannot be negative.", nameof(blockNumber));
            if (string.IsNullOrWhiteSpace(previousHash))
                throw new ArgumentException("Previous hash cannot be null or empty.", nameof(previousHash));
            if (string.IsNullOrWhiteSpace(producerAddress))
                throw new ArgumentException("Producer address cannot be null or empty.", nameof(producerAddress));

            BlockNumber = blockNumber;
            PreviousHash = previousHash;
            ProducerAddress = producerAddress;
            Timestamp = DateTimeOffset.UtcNow;
            Hash = string.Empty;
            MerkleRoot = string.Empty;
        }

        public bool IsValid => IsSealed && !string.IsNullOrEmpty(Hash) && !string.IsNullOrEmpty(MerkleRoot);

        protected void Seal(string hash, string merkleRoot)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Hash cannot be null or empty.", nameof(hash));
            if (string.IsNullOrWhiteSpace(merkleRoot))
                throw new ArgumentException("Merkle root cannot be null or empty.", nameof(merkleRoot));

            lock (WriteLock)
            {
                if (IsSealed)
                    throw new InvalidOperationException($"{GetType().Name} has already been sealed.");

                Hash = hash;
                MerkleRoot = merkleRoot;
                IsSealed = true;
            }
        }

        protected void ThrowIfSealed()
        {
            if (IsSealed)
                throw new InvalidOperationException($"Cannot modify a sealed {GetType().Name}.");
        }
    }
}