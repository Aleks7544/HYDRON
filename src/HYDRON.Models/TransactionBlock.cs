using System.Numerics;

namespace HYDRON.Models
{
    public sealed class TransactionBlock : Block
    {
        public Atomos ElectricityPriceAtomosPerEv { get; private init; }

        public string StateRoot { get; private set; }

        private readonly List<Transaction> _transactions = [];
        public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();
        public int TransactionCount => _transactions.Count;

        public Atomos GetTotalFees() =>
            _transactions.Aggregate(Atomos.Zero, (acc, tx) => acc + tx.Fee);

        public TransactionBlock(
            BigInteger blockNumber,
            string previousHash,
            string coreValidatorAddress,
            Atomos electricityPriceAtomosPerEv)
            : base(blockNumber, previousHash, coreValidatorAddress)
        {
            if (electricityPriceAtomosPerEv <= Atomos.Zero)
                throw new ArgumentException("Electricity price must be greater than zero.", nameof(electricityPriceAtomosPerEv));

            ElectricityPriceAtomosPerEv = electricityPriceAtomosPerEv;
            StateRoot = string.Empty;
        }

        public void AddTransaction(Transaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            if (!transaction.IsFinalized)
                throw new InvalidOperationException("Only finalized transactions may be added to a block.");
            if (string.IsNullOrEmpty(transaction.Hash))
                throw new InvalidOperationException("Transaction must have a hash before being added to a block.");
            if (!transaction.IsSignedByReceiver())
                throw new InvalidOperationException("Transaction is missing required receiver confirmation.");

            lock (WriteLock)
            {
                ThrowIfSealed();
                if (_transactions.Count >= Capacity)
                    throw new InvalidOperationException($"TransactionBlock is full ({Capacity} transactions).");
                if (_transactions.Any(t => t.Hash == transaction.Hash))
                    throw new InvalidOperationException($"Transaction {transaction.Hash} is already in this block.");

                _transactions.Add(transaction);
            }
        }

        public void Seal(string hash, string merkleRoot, string stateRoot)
        {
            if (string.IsNullOrWhiteSpace(stateRoot))
                throw new ArgumentException("State root cannot be null or empty.", nameof(stateRoot));

            lock (WriteLock)
            {
                ThrowIfSealed();
                if (_transactions.Count == 0)
                    throw new InvalidOperationException("Cannot seal an empty TransactionBlock.");

                StateRoot = stateRoot;
            }

            base.Seal(hash, merkleRoot);
        }

        public new bool IsValid =>
            base.IsValid &&
            !string.IsNullOrEmpty(StateRoot) &&
            _transactions.Count > 0 &&
            _transactions.All(t => t.IsFinalized);

        public override string ToString() =>
            $"TX BLOCK (#{BlockNumber} | Hash: {Hash} | Producer: {ProducerAddress} | Txs: {TransactionCount}/{Capacity} | ElecPrice: {ElectricityPriceAtomosPerEv} atomos/eV | Timestamp: {Timestamp})";
    }
}