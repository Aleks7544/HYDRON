using System.Numerics;

namespace HYDRON.Models
{
    public class BlockReward
    {
        public Guid Id { get; private set; }

        public BigInteger BlockNumber { get; private set; }
        public DateTimeOffset IssuedAt { get; private set; }
        public RewardStatus Status { get; private set; } = RewardStatus.Pending;
        public DateTimeOffset? SettledAt { get; private set; }

        public int CoreCapacityAtBlock { get; private set; }
        public int TotalValidatorsAtBlock { get; private set; }
        public double AverageValidationTimeMs { get; private set; }

        public Atomos TotalCoreBlockReward { get; private set; }
        public Atomos TotalEdgeBlockReward { get; private set; }
        public Atomos TotalValidationReward { get; private set; }
        public Atomos TotalFeeReward { get; private set; }
        public Atomos TotalMinted { get; private set; }

        private readonly List<ValidatorReward> _validatorRewards = [];
        public IReadOnlyList<ValidatorReward> ValidatorRewards => _validatorRewards.AsReadOnly();

        public BlockReward(
            BigInteger blockNumber,
            int coreCapacityAtBlock,
            int totalValidatorsAtBlock,
            double averageValidationTimeMs,
            Atomos totalCoreBlockReward,
            Atomos totalEdgeBlockReward,
            Atomos totalValidationReward,
            Atomos totalFeeReward,
            IEnumerable<ValidatorReward> validatorRewards)
        {
            if (blockNumber < BigInteger.Zero)
                throw new ArgumentException("Block number cannot be negative.", nameof(blockNumber));
            if (coreCapacityAtBlock < 0)
                throw new ArgumentException("Core capacity cannot be negative.", nameof(coreCapacityAtBlock));
            if (totalValidatorsAtBlock < 0)
                throw new ArgumentException("Total validators count cannot be negative.", nameof(totalValidatorsAtBlock));
            if (averageValidationTimeMs < 0)
                throw new ArgumentException("Average validation time cannot be negative.", nameof(averageValidationTimeMs));
            ArgumentNullException.ThrowIfNull(validatorRewards);

            Id = Guid.CreateVersion7(DateTimeOffset.UtcNow);
            IssuedAt = DateTimeOffset.UtcNow;
            BlockNumber = blockNumber;
            CoreCapacityAtBlock = coreCapacityAtBlock;
            TotalValidatorsAtBlock = totalValidatorsAtBlock;
            AverageValidationTimeMs = averageValidationTimeMs;
            TotalCoreBlockReward = totalCoreBlockReward;
            TotalEdgeBlockReward = totalEdgeBlockReward;
            TotalValidationReward = totalValidationReward;
            TotalFeeReward = totalFeeReward;
            TotalMinted = totalCoreBlockReward + totalEdgeBlockReward + totalValidationReward;
            _validatorRewards.AddRange(validatorRewards);
        }

        public void Settle()
        {
            if (Status == RewardStatus.Settled)
                throw new InvalidOperationException("Block reward has already been settled.");

            Status = RewardStatus.Settled;
            SettledAt = DateTimeOffset.UtcNow;
        }

        public override string ToString() =>
            $"BLOCK REWARD (Block: #{BlockNumber} | Status: {Status} | TotalMinted: {TotalMinted} | Validators: {ValidatorRewards.Count} | IssuedAt: {IssuedAt} | SettledAt: {SettledAt?.ToString() ?? "Pending"})";
    }

    public class ValidatorReward
    {
        public string ValidatorAddress { get; private set; }
        public ValidatorTier Tier { get; private set; }
        public int TransactionsValidated { get; private set; }

        public Atomos BlockRewardAmount { get; private set; }
        public Atomos ValidationReward { get; private set; }
        public Atomos FeeReward { get; private set; }
        public Atomos TotalReward { get; private set; }

        public ValidatorReward(
            string validatorAddress,
            ValidatorTier tier,
            int transactionsValidated,
            Atomos blockRewardAmount,
            Atomos validationReward,
            Atomos feeReward)
        {
            if (string.IsNullOrWhiteSpace(validatorAddress))
                throw new ArgumentException("Validator address cannot be null or empty.", nameof(validatorAddress));
            if (transactionsValidated < 0)
                throw new ArgumentException("Transactions validated count cannot be negative.", nameof(transactionsValidated));

            ValidatorAddress = validatorAddress;
            Tier = tier;
            TransactionsValidated = transactionsValidated;
            BlockRewardAmount = blockRewardAmount;
            ValidationReward = validationReward;
            FeeReward = feeReward;
            TotalReward = blockRewardAmount + validationReward + feeReward;
        }

        public override string ToString() =>
            $"VALIDATOR REWARD (Address: {ValidatorAddress} | Tier: {Tier} | Txs: {TransactionsValidated} | Total: {TotalReward})";
    }
}