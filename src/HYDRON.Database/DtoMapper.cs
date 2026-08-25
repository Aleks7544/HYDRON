using HYDRON.Database.Dtos;
using HYDRON.Models;
using System.Numerics;

namespace HYDRON.Database
{
    internal static class DtoMapper
    {
        public static AccountDto ToDto(Account a) => new()
        {
            Address = a.Address,
            PublicKey = a.PublicKey,
            StealthPublicKey = a.StealthPublicKey,
            Handle = a.Handle,
            Balance = ((BigInteger)a.Balance).ToString(),
            Nonce = a.Nonce.ToString(),
        };

        public static Account FromDto(AccountDto dto) =>
            Account.Restore(
                dto.Address,
                dto.PublicKey,
                dto.StealthPublicKey,
                dto.Handle,
                new Atomos(BigInteger.Parse(dto.Balance)),
                BigInteger.Parse(dto.Nonce));

        public static ValidatorDto ToDto(Validator v) => new()
        {
            Address = v.Address,
            PublicKey = v.PublicKey,
            StealthPublicKey = v.StealthPublicKey,
            Handle = v.Handle,
            Balance = ((BigInteger)v.Balance).ToString(),
            Nonce = v.Nonce.ToString(),
            StakedAmount = ((BigInteger)v.StakedAmount).ToString(),
            Tier = v.Tier,
            Status = v.Status,
            CorrectVotes = v.CorrectVotes.ToString(),
            TotalVotes = v.TotalVotes.ToString(),
            TransactionsValidated = v.TransactionsValidatedCount.ToString(),
            RejectedTransactions = v.RejectedTransactionsCount.ToString(),
            TotalTransactionValue = ((BigInteger)v.TotalTransactionValue).ToString(),
            TotalRewardsEarned = ((BigInteger)v.TotalRewardsEarned).ToString(),
            TotalPenaltyAmount = ((BigInteger)v.TotalPenaltyAmount).ToString(),
            NetworkEndpointIPv4 = v.NetworkEndpointIPv4,
            NetworkEndpointIPv6 = v.NetworkEndpointIPv6,
            NetworkEndpointDns = v.NetworkEndpointDns,
            CommissionRate = v.CommissionRate,
            Description = v.Description,
            ConfirmedValidationIds = v.ConfirmedValidationIds.ToList(),
            RejectedValidationIds = v.RejectedValidationIds.ToList(),
        };

        public static Validator FromDto(ValidatorDto dto) =>
            Validator.Restore(
                dto.Address, dto.PublicKey, dto.StealthPublicKey, dto.Handle,
                new Atomos(BigInteger.Parse(dto.Balance)),
                BigInteger.Parse(dto.Nonce),
                new Atomos(BigInteger.Parse(dto.StakedAmount)),
                dto.Tier, dto.Status,
                BigInteger.Parse(dto.CorrectVotes),
                BigInteger.Parse(dto.TotalVotes),
                BigInteger.Parse(dto.TransactionsValidated),
                BigInteger.Parse(dto.RejectedTransactions),
                new Atomos(BigInteger.Parse(dto.TotalTransactionValue)),
                new Atomos(BigInteger.Parse(dto.TotalRewardsEarned)),
                new Atomos(BigInteger.Parse(dto.TotalPenaltyAmount)),
                dto.NetworkEndpointIPv4, dto.NetworkEndpointIPv6, dto.NetworkEndpointDns,
                dto.CommissionRate, dto.Description,
                dto.ConfirmedValidationIds, dto.RejectedValidationIds);

        public static TransactionDto ToDto(Transaction tx) => new()
        {
            Sender = tx.Sender,
            Receiver = tx.Receiver,
            Amount = ((BigInteger)tx.Amount).ToString(),
            Fee = ((BigInteger)tx.Fee).ToString(),
            Nonce = tx.Nonce.ToString(),
            SenderSignature = tx.SenderSignature,
            ReceiverSignature = tx.ReceiverSignature,
            Hash = tx.Hash,
            Status = tx.Status,
            RequiresReceiverConfirmation = tx.RequiresReceiverConfirmation,
            InitiatedAt = tx.InitiatedAt,
            IsFinalized = tx.IsFinalized,
            FinalizedAt = tx.FinalizedAt,
            Priority = tx.Priority,
            TransactionBlockNumber = tx.TransactionBlockNumber?.ToString(),
            PrivacyMode = tx.PrivacyMode,
            EphemeralPublicKey = tx.EphemeralPublicKey,
            AssignedValidators = tx.AssignedValidators.ToList(),
        };

        public static Transaction FromDto(TransactionDto dto) =>
            Transaction.Restore(
                dto.Sender, dto.Receiver,
                new Atomos(BigInteger.Parse(dto.Amount)),
                new Atomos(BigInteger.Parse(dto.Fee)),
                BigInteger.Parse(dto.Nonce),
                dto.SenderSignature, dto.ReceiverSignature,
                dto.Hash, dto.Status,
                dto.RequiresReceiverConfirmation,
                dto.InitiatedAt, dto.IsFinalized, dto.FinalizedAt,
                dto.Priority,
                dto.TransactionBlockNumber is null ? null : BigInteger.Parse(dto.TransactionBlockNumber),
                dto.PrivacyMode, dto.EphemeralPublicKey,
                dto.AssignedValidators);

        public static TransactionBlockDto ToDto(TransactionBlock b) => new()
        {
            BlockNumber = b.BlockNumber.ToString(),
            Hash = b.Hash,
            PreviousHash = b.PreviousHash,
            Timestamp = b.Timestamp,
            ProducerAddress = b.ProducerAddress,
            MerkleRoot = b.MerkleRoot,
            StateRoot = b.StateRoot,
            ElectricityPriceAtomosPerEv = ((BigInteger)b.ElectricityPriceAtomosPerEv).ToString(),
            TransactionHashes = b.Transactions.Select(t => t.Hash).ToList(),
        };

        public static TransactionBlock FromDto(TransactionBlockDto dto) =>
            TransactionBlock.Restore(
                BigInteger.Parse(dto.BlockNumber),
                dto.Hash, dto.PreviousHash,
                dto.Timestamp, dto.ProducerAddress,
                dto.MerkleRoot, dto.StateRoot,
                new Atomos(BigInteger.Parse(dto.ElectricityPriceAtomosPerEv)),
                dto.TransactionHashes);

        public static StateBlockDto ToDto(StateBlock sb) => new()
        {
            BlockNumber = sb.BlockNumber.ToString(),
            Hash = sb.Hash,
            PreviousHash = sb.PreviousHash,
            Timestamp = sb.Timestamp,
            ProducerAddress = sb.ProducerAddress,
            MerkleRoot = sb.MerkleRoot,
            GlobalStateRoot = sb.GlobalStateRoot,
            TotalFeesCollected = ((BigInteger)sb.TotalFeesCollected).ToString(),
            TransactionBlockHashes = sb.TransactionBlockHashes.ToList(),
        };

        public static StateBlock FromDto(StateBlockDto dto) =>
            StateBlock.Restore(
                BigInteger.Parse(dto.BlockNumber),
                dto.Hash, dto.PreviousHash,
                dto.Timestamp, dto.ProducerAddress,
                dto.MerkleRoot, dto.GlobalStateRoot,
                new Atomos(BigInteger.Parse(dto.TotalFeesCollected)),
                dto.TransactionBlockHashes);
    }
}