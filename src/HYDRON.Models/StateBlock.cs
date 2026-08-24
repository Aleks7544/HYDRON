using System.Numerics;

namespace HYDRON.Models
{
    public sealed class StateBlock(
        BigInteger blockNumber,
        string previousHash,
        string coreValidatorAddress)
        : Block(blockNumber, previousHash, coreValidatorAddress)
    {
        public string GlobalStateRoot { get; private set; } = string.Empty;

        public Atomos TotalFeesCollected { get; private set; } = Atomos.Zero;

        private readonly List<string> _transactionBlockHashes = [];
        public IReadOnlyList<string> TransactionBlockHashes => _transactionBlockHashes.AsReadOnly();
        public int TransactionBlockCount => _transactionBlockHashes.Count;

        public void AddTransactionBlockHash(string blockHash)
        {
            if (string.IsNullOrWhiteSpace(blockHash))
                throw new ArgumentException("Block hash cannot be null or empty.", nameof(blockHash));

            lock (WriteLock)
            {
                ThrowIfSealed();
                if (_transactionBlockHashes.Count >= Capacity)
                    throw new InvalidOperationException($"StateBlock already contains the maximum {Capacity} TransactionBlocks.");
                if (_transactionBlockHashes.Contains(blockHash, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"TransactionBlock hash {blockHash} is already registered in this StateBlock.");

                _transactionBlockHashes.Add(blockHash);
            }
        }

        public void Seal(string hash, string merkleRoot, string globalStateRoot, Atomos totalFeesCollected)
        {
            if (string.IsNullOrWhiteSpace(globalStateRoot))
                throw new ArgumentException("Global state root cannot be null or empty.", nameof(globalStateRoot));

            lock (WriteLock)
            {
                ThrowIfSealed();
                if (_transactionBlockHashes.Count != Capacity)
                    throw new InvalidOperationException(
                        $"StateBlock requires exactly {Capacity} TransactionBlocks before sealing. Current: {_transactionBlockHashes.Count}.");

                GlobalStateRoot = globalStateRoot;
                TotalFeesCollected = totalFeesCollected;
            }

            base.Seal(hash, merkleRoot);
        }

        public new bool IsValid =>
            base.IsValid &&
            !string.IsNullOrEmpty(GlobalStateRoot) &&
            _transactionBlockHashes.Count == Capacity;

        public override string ToString() =>
            $"STATE BLOCK (#{BlockNumber} | Hash: {Hash} | Producer: {ProducerAddress} | TxBlocks: {TransactionBlockCount}/{Capacity} | Fees: {TotalFeesCollected} | Timestamp: {Timestamp})";
    }
}