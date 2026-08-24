using System.Collections.Concurrent;
using System.Numerics;

namespace HYDRON.Models
{
    public class Mempool
    {
        private readonly ConcurrentDictionary<string, Transaction> _byHash = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, HashSet<string>> _bySender = new(StringComparer.OrdinalIgnoreCase);

        private readonly Lock _senderLock = new();

        public int Count => _byHash.Count;

        public bool TryEnqueue(Transaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);

            if (string.IsNullOrEmpty(transaction.Hash))
                throw new InvalidOperationException("Transaction must have a hash set before entering the mempool.");
            if (transaction.Status != TransactionStatus.PendingValidation)
                throw new InvalidOperationException(
                    $"Only transactions in {TransactionStatus.PendingValidation} status may enter the mempool. Current transaction status: {transaction.Status}.");

            if (!_byHash.TryAdd(transaction.Hash, transaction))
                return false;

            lock (_senderLock)
            {
                if (!_bySender.TryGetValue(transaction.Sender, out HashSet<string>? hashes))
                {
                    hashes = [];
                    _bySender[transaction.Sender] = hashes;
                }
                hashes.Add(transaction.Hash);
            }

            return true;
        }

        public bool TryRemove(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Hash cannot be null or empty.", nameof(hash));

            if (!_byHash.TryRemove(hash, out Transaction? tx))
                return false;

            lock (_senderLock)
            {
                if (!_bySender.TryGetValue(tx.Sender, out HashSet<string>? hashes)) return true;
                hashes.Remove(hash);
                if (hashes.Count == 0)
                    _bySender.TryRemove(tx.Sender, out _);
            }

            return true;
        }

        public bool Contains(string hash) =>
            !string.IsNullOrWhiteSpace(hash) && _byHash.ContainsKey(hash);

        public bool TryGet(string hash, out Transaction? transaction) =>
            _byHash.TryGetValue(hash, out transaction);

        public IReadOnlyList<Transaction> PeekForBlock(int maxCount)
        {
            if (maxCount <= 0)
                throw new ArgumentException("Max count must be greater than zero.", nameof(maxCount));

            return _byHash.Values
                .Where(tx => tx.Status == TransactionStatus.PendingValidation)
                .OrderByDescending(tx => (int)(tx.Priority ?? Priority.Low))
                .ThenByDescending(tx => tx.Fee)
                .ThenBy(tx => tx.InitiatedAt)
                .Take(maxCount)
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlySet<string> GetHashesBySender(string senderAddress)
        {
            if (string.IsNullOrWhiteSpace(senderAddress))
                throw new ArgumentException("Sender address cannot be null or empty.", nameof(senderAddress));

            lock (_senderLock)
            {
                return _bySender.TryGetValue(senderAddress, out HashSet<string>? hashes)
                    ? hashes.ToHashSet()
                    : (IReadOnlySet<string>)new HashSet<string>();
            }
        }

        public int EvictStaleBySender(string senderAddress, BigInteger confirmedNonce)
        {
            if (string.IsNullOrWhiteSpace(senderAddress))
                throw new ArgumentException("Sender address cannot be null or empty.", nameof(senderAddress));

            int evicted = 0;
            lock (_senderLock)
            {
                if (!_bySender.TryGetValue(senderAddress, out HashSet<string>? hashes))
                    return 0;

                List<string> toRemove = [];
                foreach (string hash in hashes)
                {
                    if (_byHash.TryGetValue(hash, out Transaction? tx) && tx.Nonce < confirmedNonce)
                        toRemove.Add(hash);
                }

                foreach (string hash in toRemove)
                {
                    _byHash.TryRemove(hash, out _);
                    hashes.Remove(hash);
                    evicted++;
                }

                if (hashes.Count == 0)
                    _bySender.TryRemove(senderAddress, out _);
            }

            return evicted;
        }

        public void Clear()
        {
            _byHash.Clear();
            lock (_senderLock)
                _bySender.Clear();
        }

        public override string ToString() =>
            $"MEMPOOL (Pending: {Count} transactions)";
    }
}