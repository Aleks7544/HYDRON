using HYDRON.Models;

namespace HYDRON.Database.Repositories
{
    public interface IAccountRepository
    {
        void Save(Account account);
        Account? GetByAddress(string address);
        bool Exists(string address);
        IEnumerable<Account> GetAll();
    }
}