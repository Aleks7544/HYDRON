using HYDRON.Models;

namespace HYDRON.Database.Repositories
{
    public interface IValidatorRepository
    {
        void Save(Validator validator);
        Validator? GetByAddress(string address);
        bool Exists(string address);
        IEnumerable<Validator> GetAll();
        IEnumerable<Validator> GetActive();
        IEnumerable<Validator> GetByTier(ValidatorTier tier);
    }
}