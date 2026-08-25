using HYDRON.Database.Dtos;
using HYDRON.Database.Serialization;
using HYDRON.Models;
using System.Numerics;
using System.Text;

namespace HYDRON.Database.Repositories
{
    public sealed class BlockRepository(IDataStore store, ISerializer serializer) : IBlockRepository
    {
        public void SaveTransactionBlock(TransactionBlock block)
        {
            ArgumentNullException.ThrowIfNull(block);
            if (!block.IsSealed)
                throw new InvalidOperationException("Only sealed TransactionBlocks may be persisted.");

            byte[] data = serializer.Serialize(DtoMapper.ToDto(block));
            BigInteger currentLatest = GetLatestTransactionBlockNumber();

            List<(string, byte[]?)> ops =
            [
                (KeyScheme.TransactionBlock(block.BlockNumber), data),
                (KeyScheme.TransactionBlockByHash(block.Hash), data),
            ];

            if (block.BlockNumber > currentLatest)
                ops.Add((KeyScheme.LatestTransactionBlockNumber, Encode(block.BlockNumber)));

            store.WriteBatch(ops);
        }

        public TransactionBlock? GetTransactionBlockByNumber(BigInteger blockNumber)
        {
            if (!store.TryGet(KeyScheme.TransactionBlock(blockNumber), out byte[]? data) || data is null)
                return null;
            return DtoMapper.FromDto(serializer.Deserialize<TransactionBlockDto>(data));
        }

        public TransactionBlock? GetTransactionBlockByHash(string hash)
        {
            if (!store.TryGet(KeyScheme.TransactionBlockByHash(hash), out byte[]? data) || data is null)
                return null;
            return DtoMapper.FromDto(serializer.Deserialize<TransactionBlockDto>(data));
        }

        public BigInteger GetLatestTransactionBlockNumber()
        {
            if (!store.TryGet(KeyScheme.LatestTransactionBlockNumber, out byte[]? data) || data is null)
                return BigInteger.MinusOne;
            return Decode(data);
        }

        public void SaveStateBlock(StateBlock block)
        {
            ArgumentNullException.ThrowIfNull(block);
            if (!block.IsSealed)
                throw new InvalidOperationException("Only sealed StateBlocks may be persisted.");

            byte[] data = serializer.Serialize(DtoMapper.ToDto(block));
            BigInteger currentLatest = GetLatestStateBlockNumber();

            List<(string, byte[]?)> ops =
            [
                (KeyScheme.StateBlock(block.BlockNumber), data),
                (KeyScheme.StateBlockByHash(block.Hash), data),
            ];

            if (block.BlockNumber > currentLatest)
                ops.Add((KeyScheme.LatestStateBlockNumber, Encode(block.BlockNumber)));

            store.WriteBatch(ops);
        }

        public StateBlock? GetStateBlockByNumber(BigInteger blockNumber)
        {
            if (!store.TryGet(KeyScheme.StateBlock(blockNumber), out byte[]? data) || data is null)
                return null;
            return DtoMapper.FromDto(serializer.Deserialize<StateBlockDto>(data));
        }

        public StateBlock? GetStateBlockByHash(string hash)
        {
            if (!store.TryGet(KeyScheme.StateBlockByHash(hash), out byte[]? data) || data is null)
                return null;
            return DtoMapper.FromDto(serializer.Deserialize<StateBlockDto>(data));
        }

        public BigInteger GetLatestStateBlockNumber()
        {
            if (!store.TryGet(KeyScheme.LatestStateBlockNumber, out byte[]? data) || data is null)
                return BigInteger.MinusOne;
            return Decode(data);
        }

        private static byte[] Encode(BigInteger value) =>
            Encoding.UTF8.GetBytes(value.ToString());

        private static BigInteger Decode(byte[] data) =>
            BigInteger.Parse(Encoding.UTF8.GetString(data));
    }
}