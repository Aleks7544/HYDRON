using HYDRON.Database.Dtos;
using HYDRON.Database.Serialization;
using HYDRON.Models;

namespace HYDRON.Database.Repositories
{
    public sealed class AccountRepository(IDataStore store, ISerializer serializer) : IAccountRepository
    {
        public void Save(Account account)
        {
            ArgumentNullException.ThrowIfNull(account);
            store.Put(KeyScheme.Account(account.Address),
                       serializer.Serialize(DtoMapper.ToDto(account)));
        }

        public Account? GetByAddress(string address)
        {
            if (!store.TryGet(KeyScheme.Account(address), out byte[]? data) || data is null)
                return null;
            return DtoMapper.FromDto(serializer.Deserialize<AccountDto>(data));
        }

        public bool Exists(string address) => store.Exists(KeyScheme.Account(address));

        public IEnumerable<Account> GetAll()
        {
            foreach ((_, byte[]? value) in store.Iterate(KeyScheme.AccountPrefix))
            {
                if (value is null) continue;
                yield return DtoMapper.FromDto(serializer.Deserialize<AccountDto>(value));
            }
        }
    }
}