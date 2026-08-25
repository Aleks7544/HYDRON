namespace HYDRON.Database
{
    public interface IDataStore : IDisposable
    {
        void Put(string key, byte[] value);
        bool TryGet(string key, out byte[]? value);
        void Delete(string key);
        bool Exists(string key);

        void WriteBatch(IReadOnlyList<(string key, byte[]? value)> operations);

        IEnumerable<(string key, byte[]? value)> Iterate(string prefix, bool includeValues = true);
    }
}