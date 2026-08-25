using HYDRON.Models;

namespace HYDRON.Database.Repositories
{
    public interface ITransactionRepository
    {
        void Save(Transaction transaction);
        Transaction? GetByHash(string hash);
        bool Exists(string hash);
        IEnumerable<Transaction> GetBySender(string senderAddress);
        IEnumerable<Transaction> GetAll();
    }
}