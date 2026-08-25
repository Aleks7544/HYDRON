using HYDRON.Database.Dtos;
using HYDRON.Database.Serialization;
using HYDRON.Models;

namespace HYDRON.Database.Repositories
{
    public sealed class TransactionRepository(IDataStore store, ISerializer serializer) : ITransactionRepository
    {
        public void Save(Transaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            if (string.IsNullOrEmpty(transaction.Hash))
                throw new InvalidOperationException("Transaction must have a hash before being persisted.");

            byte[] data = serializer.Serialize(DtoMapper.ToDto(transaction));

            store.WriteBatch([
                (KeyScheme.Transaction(transaction.Hash), data),
                (KeyScheme.TransactionBySender(transaction.Sender, transaction.Hash), data),
            ]);
        }

        public Transaction? GetByHash(string hash)
        {
            if (!store.TryGet(KeyScheme.Transaction(hash), out byte[]? data) || data is null)
                return null;
            return DtoMapper.FromDto(serializer.Deserialize<TransactionDto>(data));
        }

        public bool Exists(string hash) => store.Exists(KeyScheme.Transaction(hash));

        public IEnumerable<Transaction> GetBySender(string senderAddress)
        {
            foreach ((_, byte[]? value) in store.Iterate(KeyScheme.TransactionBySenderPrefix(senderAddress)))
            {
                if (value is null) continue;
                yield return DtoMapper.FromDto(serializer.Deserialize<TransactionDto>(value));
            }
        }

        public IEnumerable<Transaction> GetAll()
        {
            foreach ((_, byte[]? value) in store.Iterate(KeyScheme.TransactionPrefix))
            {
                if (value is null) continue;
                yield return DtoMapper.FromDto(serializer.Deserialize<TransactionDto>(value));
            }
        }
    }
}