using RocksDbSharp;
using System.Text;

namespace HYDRON.Database
{
    public sealed class RocksDbDataStore : IDataStore
    {
        private readonly RocksDb _db;
        private bool _disposed;

        public RocksDbDataStore(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Database path cannot be null or empty.", nameof(path));

            Directory.CreateDirectory(path);

            DbOptions options = new DbOptions()
                .SetCreateIfMissing(true)
                .SetCreateMissingColumnFamilies(true);

            _db = RocksDb.Open(options, path);
        }

        public void Put(string key, byte[] value)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _db.Put(Encode(key), value);
        }

        public bool TryGet(string key, out byte[]? value)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            value = _db.Get(Encode(key));
            return value is not null;
        }

        public void Delete(string key)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _db.Remove(Encode(key));
        }

        public bool Exists(string key)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _db.Get(Encode(key)) is not null;
        }

        public void WriteBatch(IReadOnlyList<(string key, byte[]? value)> operations)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(operations);

            using WriteBatch batch = new();
            foreach ((string key, byte[]? value) in operations)
            {
                if (value is null)
                    batch.Delete(Encode(key));
                else
                    batch.Put(Encode(key), value);
            }
            _db.Write(batch);
        }

        public IEnumerable<(string key, byte[]? value)> Iterate(string prefix, bool includeValues = true)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            byte[] prefixBytes = Encode(prefix);
            using Iterator iterator = _db.NewIterator();
            iterator.Seek(prefixBytes);

            while (iterator.Valid())
            {
                string key = Decode(iterator.Key());
                if (!key.StartsWith(prefix, StringComparison.Ordinal))
                    yield break;

                byte[]? value = includeValues ? iterator.Value() : null;
                yield return (key, value);
                iterator.Next();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _db.Dispose();
            _disposed = true;
        }

        private static byte[] Encode(string key) => Encoding.UTF8.GetBytes(key);
        private static string Decode(byte[] key) => Encoding.UTF8.GetString(key);
    }
}