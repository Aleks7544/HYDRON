using HYDRON.Models;
using System.Numerics;

namespace HYDRON.Database.Repositories
{
    public interface IBlockRepository
    {
        void SaveTransactionBlock(TransactionBlock block);
        TransactionBlock? GetTransactionBlockByNumber(BigInteger blockNumber);
        TransactionBlock? GetTransactionBlockByHash(string hash);
        BigInteger GetLatestTransactionBlockNumber();

        void SaveStateBlock(StateBlock block);
        StateBlock? GetStateBlockByNumber(BigInteger blockNumber);
        StateBlock? GetStateBlockByHash(string hash);
        BigInteger GetLatestStateBlockNumber();
    }
}