using HYDRON.Database.Dtos;
using HYDRON.Database.Serialization;
using HYDRON.Models;

namespace HYDRON.Database.Repositories
{
    public sealed class ValidatorRepository(IDataStore store, ISerializer serializer) : IValidatorRepository
    {
        public void Save(Validator validator)
        {
            ArgumentNullException.ThrowIfNull(validator);
            store.Put(KeyScheme.Validator(validator.Address),
                       serializer.Serialize(DtoMapper.ToDto(validator)));
        }

        public Validator? GetByAddress(string address)
        {
            if (!store.TryGet(KeyScheme.Validator(address), out byte[]? data) || data is null)
                return null;
            return DtoMapper.FromDto(serializer.Deserialize<ValidatorDto>(data));
        }

        public bool Exists(string address) => store.Exists(KeyScheme.Validator(address));

        public IEnumerable<Validator> GetAll()
        {
            foreach ((_, byte[]? value) in store.Iterate(KeyScheme.ValidatorPrefix))
            {
                if (value is null) continue;
                yield return DtoMapper.FromDto(serializer.Deserialize<ValidatorDto>(value));
            }
        }

        public IEnumerable<Validator> GetActive() =>
            GetAll().Where(v => v.Status == ValidatorStatus.Active);

        public IEnumerable<Validator> GetByTier(ValidatorTier tier) =>
            GetAll().Where(v => v.Tier == tier);
    }
}