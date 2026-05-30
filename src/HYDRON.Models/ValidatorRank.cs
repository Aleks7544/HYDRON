namespace HYDRON.Models
{
    public record ValidatorRank
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
}