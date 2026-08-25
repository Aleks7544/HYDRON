using System.Text.Json;
using System.Text.Json.Serialization;

namespace HYDRON.Database.Serialization
{
    public sealed class HydronJsonSerializer : ISerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public byte[] Serialize<T>(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return JsonSerializer.SerializeToUtf8Bytes(value, Options);
        }

        public T Deserialize<T>(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            return JsonSerializer.Deserialize<T>(data, Options)
                   ?? throw new InvalidOperationException($"Deserialization of {typeof(T).Name} returned null.");
        }
    }
}