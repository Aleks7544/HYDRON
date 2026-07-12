using System.Numerics;

namespace HYDRON.Models
{
    public class TransactionBlock
    {
        public BigInteger BlockNumber { get; private set; }
        public string Hash { get; private set; }
        public string PreviousHash { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }
        public string ValidatorAddress { get; private set; }
        public string MerkleRoot { get; private set; }
        public string StateRoot { get; private set; }

        private bool _sealed;
        private readonly Lock _writeLock = new();

        private readonly List<Transaction> _transactions = [];
        public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();

        public int TransactionCount => _transactions.Count;

        public Atomos GetTotalFees() =>
            _transactions.Aggregate(Atomos.Zero, (current, tx) => current + tx.Fee);

        public TransactionBlock(BigInteger blockNumber, string previousHash, string validatorAddress)
        {
            if (blockNumber < BigInteger.Zero)
                throw new ArgumentException("Block number cannot be negative.", nameof(blockNumber));
            if (string.IsNullOrWhiteSpace(previousHash))
                throw new ArgumentException("Previous hash cannot be null or empty.", nameof(previousHash));
            if (string.IsNullOrWhiteSpace(validatorAddress))
                throw new ArgumentException("Validator address cannot be null or empty.", nameof(validatorAddress));

            BlockNumber = blockNumber;
            PreviousHash = previousHash;
            ValidatorAddress = validatorAddress;
            Timestamp = DateTimeOffset.UtcNow;
            Hash = string.Empty;
            MerkleRoot = string.Empty;
            StateRoot = string.Empty;
        }

        public void AddTransaction(Transaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            if (!transaction.IsFinalized)
                throw new InvalidOperationException("Only finalized transactions may be added to a block.");
            if (string.IsNullOrEmpty(transaction.Hash))
                throw new InvalidOperationException("Transaction must have a hash set before being added to a block.");
            if (!transaction.IsSignedByReceiver())
                throw new InvalidOperationException("Transaction requires receiver confirmation that has not been provided.");

            lock (_writeLock)
            {
                if (_sealed)
                    throw new InvalidOperationException("Cannot add transactions to a sealed block.");
                if (_transactions.Any(t => t.Hash == transaction.Hash))
                    throw new InvalidOperationException($"Transaction {transaction.Hash} is already in this block.");

                _transactions.Add(transaction);
            }
        }

        public void Seal(string hash, string merkleRoot, string stateRoot)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Hash cannot be null or empty.", nameof(hash));
            if (string.IsNullOrWhiteSpace(merkleRoot))
                throw new ArgumentException("Merkle root cannot be null or empty.", nameof(merkleRoot));
            if (string.IsNullOrWhiteSpace(stateRoot))
                throw new ArgumentException("State root cannot be null or empty.", nameof(stateRoot));

            lock (_writeLock)
            {
                if (_sealed)
                    throw new InvalidOperationException("Block has already been sealed.");
                if (_transactions.Count == 0)
                    throw new InvalidOperationException("Cannot seal an empty block.");

                Hash = hash;
                MerkleRoot = merkleRoot;
                StateRoot = stateRoot;
                _sealed = true;
            }
        }

        public void SetHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Hash cannot be null or empty.", nameof(hash));

            lock (_writeLock)
            {
                if (_sealed)
                    throw new InvalidOperationException("Block has already been sealed.");
                if (!string.IsNullOrEmpty(Hash))
                    throw new InvalidOperationException("Block hash has already been set and cannot be changed.");
                Hash = hash;
            }
        }

        public void SetMerkleRoot(string merkleRoot)
        {
            lock (_writeLock)
            {
                if (_sealed)
                    throw new InvalidOperationException("Block has already been sealed.");
                if (!string.IsNullOrEmpty(MerkleRoot))
                    throw new InvalidOperationException("Merkle root has already been set and cannot be changed.");
                if (string.IsNullOrWhiteSpace(merkleRoot))
                    throw new ArgumentException("Merkle root cannot be null or empty.", nameof(merkleRoot));

                MerkleRoot = merkleRoot;
            }
        }

        public void SetStateRoot(string stateRoot)
        {
            lock (_writeLock)
            {
                if (_sealed)
                    throw new InvalidOperationException("Block has already been sealed.");
                if (!string.IsNullOrEmpty(StateRoot))
                    throw new InvalidOperationException("State root has already been set and cannot be changed.");
                if (string.IsNullOrWhiteSpace(stateRoot))
                    throw new ArgumentException("State root cannot be null or empty.", nameof(stateRoot));

                StateRoot = stateRoot;
            }
        }

        public bool IsValid() =>
            _sealed &&
            !string.IsNullOrEmpty(Hash) &&
            !string.IsNullOrEmpty(MerkleRoot) &&
            !string.IsNullOrEmpty(StateRoot) &&
            _transactions.Count > 0 &&
            _transactions.All(t => t.IsFinalized);

        public override string ToString() =>
            $"BLOCK (#{BlockNumber} | Hash: {Hash} | Validator: {ValidatorAddress} | Txs: {TransactionCount} | Timestamp: {Timestamp})";
    }
}
