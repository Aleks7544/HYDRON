namespace HYDRON.Models
{
    public record ValidatorRankScore
    {
        public required string ValidatorAddress { get; init; }
        public required Atomos StakedAmount { get; init; }
        public required double AvgValidationSpeedMs { get; init; }
        public required long ValidationActivityCount { get; init; }
        public required double ReputationNormalized { get; init; }
        public required int BlocksObserved { get; init; }
        public required DateTimeOffset ComputedAt { get; init; }
        public required double FinalRank { get; init; }
        public required ValidatorTier Tier { get; init; }
    }

    public static class ValidatorRankCalculator
    {
        public const double StakeWeight = 1.000;
        public const double SpeedWeight = 0.500;
        public const double ActivityWeight = 0.250;
        public const double UptimeWeight = 0.125;

        private const double DefaultSpeedMs = 1000.0;

        public static List<ValidatorRankScore> Rank(
            IEnumerable<Validator> allValidators,
            int coreCapacity,
            IEnumerable<Validation> validationsFromLastNBlocks,
            int blocksObserved)
        {
            List<Validator> validators = allValidators.ToList();
            if (validators.Count == 0) return [];

            if (coreCapacity < 0)
                throw new ArgumentException("Core capacity cannot be negative.", nameof(coreCapacity));

            if (blocksObserved <= 0)
                throw new ArgumentException("Blocks observed must be greater than zero.", nameof(blocksObserved));

            List<Validation> validationList = validationsFromLastNBlocks.ToList();
            Dictionary<string, double> speedMap = ComputeAvgSpeedPerValidator(validationList);
            Dictionary<string, long> activityMap = ComputeActivityPerValidator(validationList);

            List<double> stakedAmounts = validators.Select(v => (double)v.StakedAmount).ToList();
            List<double> speeds = validators.Select(v => speedMap.GetValueOrDefault(v.Address, DefaultSpeedMs)).ToList();
            List<double> activities = validators.Select(v => (double)activityMap.GetValueOrDefault(v.Address, 0)).ToList();
            List<double> reputations = validators.Select(v => v.ReputationScore / 100.0).ToList();

            double[] normStake = Normalize(stakedAmounts);
            double[] normSpeed = NormalizeInverted(speeds);
            double[] normActivity = Normalize(activities);
            double[] normReputation = reputations.ToArray();

            List<ValidatorRankScore> scores = [];

            for (int i = 0; i < validators.Count; i++)
            {
                Validator validator = validators[i];
                double finalRank = (StakeWeight * normStake[i])
                                 + (SpeedWeight * normSpeed[i])
                                 + (ActivityWeight * normActivity[i])
                                 + (UptimeWeight * normReputation[i]);

                scores.Add(new ValidatorRankScore
                {
                    ValidatorAddress = validator.Address,
                    StakedAmount = validator.StakedAmount,
                    AvgValidationSpeedMs = speedMap.GetValueOrDefault(validator.Address, DefaultSpeedMs),
                    ValidationActivityCount = activityMap.GetValueOrDefault(validator.Address, 0),
                    ReputationNormalized = reputations[i],
                    BlocksObserved = blocksObserved,
                    ComputedAt = DateTimeOffset.UtcNow,
                    FinalRank = finalRank,
                    Tier = ValidatorTier.Edge
                });
            }

            List<ValidatorRankScore> ranked = scores.OrderByDescending(s => s.FinalRank).ToList();

            for (int i = 0; i < ranked.Count; i++)
            {
                ValidatorRankScore score = ranked[i];
                ranked[i] = score with { Tier = i < coreCapacity ? ValidatorTier.Core : ValidatorTier.Edge };
            }

            return ranked;
        }

        private static Dictionary<string, double> ComputeAvgSpeedPerValidator(
            IEnumerable<Validation> validations)
        {
            return validations
                .Where(v => v.ValidationSpeedMs.HasValue)
                .GroupBy(v => v.ValidatorAddress)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(v => v.ValidationSpeedMs!.Value));
        }

        private static Dictionary<string, long> ComputeActivityPerValidator(
            IEnumerable<Validation> validations)
        {
            return validations
                .GroupBy(v => v.ValidatorAddress)
                .ToDictionary(
                    g => g.Key,
                    g => (long)g.Count());
        }

        private static double[] Normalize(List<double> values)
        {
            double min = values.Min();
            double max = values.Max();
            double range = max - min;

            return range == 0 ? values.Select(_ => 1.0).ToArray() : values.Select(v => (v - min) / range).ToArray();
        }

        private static double[] NormalizeInverted(List<double> values)
        {
            double min = values.Min();
            double max = values.Max();
            double range = max - min;

            return range == 0 ? values.Select(_ => 1.0).ToArray() : values.Select(v => 1.0 - (v - min) / range).ToArray();
        }
    }
}