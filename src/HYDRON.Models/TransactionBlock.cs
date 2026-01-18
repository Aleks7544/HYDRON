using System.Numerics;

namespace Hydron.Models
{
    public class TransactionBlock
    {
        public BigInteger BlockNumber { get; set; }
        public string Hash { get; set; }
        public string PreviousHash { get; set; }
        public DateTime Timestamp { get; set; }
        public string Validator { get; set; }
        public List<Transaction> Transactions { get; set; }
        public string MerkleRoot { get; set; }
        public string StateRoot { get; set; }

        public TransactionBlock(BigInteger blockNumber, string previousHash, string validator)
        {
            if (string.IsNullOrWhiteSpace(previousHash))
                throw new ArgumentException("Previous hash cannot be null or empty.", nameof(previousHash));
            if (string.IsNullOrWhiteSpace(validator))
                throw new ArgumentException("Validator cannot be null or empty.", nameof(validator));

            BlockNumber = blockNumber;
            PreviousHash = previousHash;
            Validator = validator;
            Timestamp = DateTime.UtcNow;
            Hash = string.Empty;
            Transactions = [];
            MerkleRoot = string.Empty;
            StateRoot = string.Empty;
        }

        public void AddTransaction(Transaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);

            if (!transaction.IsSigned())
                throw new InvalidOperationException("Transaction must be signed before adding to block.");

            Transactions.Add(transaction);
        }

        public int TransactionCount => Transactions.Count;

        public Atomos GetTotalFees() =>
            Transactions.Aggregate<Transaction, Atomos>(new Atomos(new BigInteger(0)),
                (current, tx) => current + tx.Fee);

        public void SetHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Hash cannot be null or empty.", nameof(hash));

            Hash = hash;
        }

        public void SetMerkleRoot(string merkleRoot)
        {
            if (string.IsNullOrWhiteSpace(merkleRoot))
                throw new ArgumentException("Merkle root cannot be null or empty.", nameof(merkleRoot));

            MerkleRoot = merkleRoot;
        }

        public void SetStateRoot(string stateRoot)
        {
            if (string.IsNullOrWhiteSpace(stateRoot))
                throw new ArgumentException("State root cannot be null or empty.", nameof(stateRoot));

            StateRoot = stateRoot;
        }

        public bool IsValid() => !string.IsNullOrEmpty(Hash)
                   && !string.IsNullOrEmpty(MerkleRoot)
                   && !string.IsNullOrEmpty(StateRoot)
                   && Transactions.Count > 0;

        public override string ToString() => $"Block #{BlockNumber} | Hash: {Hash[..Math.Min(8, Hash.Length)]} | Txs: {Transactions.Count}";
    }
}