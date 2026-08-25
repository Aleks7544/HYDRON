using HYDRON.Models;

namespace HYDRON.Database.Dtos
{
    internal sealed class ValidatorDto : AccountDto
    {
        public string StakedAmount { get; set; } = "0";
        public ValidatorTier Tier { get; set; }
        public ValidatorStatus Status { get; set; }
        public string CorrectVotes { get; set; } = "0";
        public string TotalVotes { get; set; } = "0";
        public string TransactionsValidated { get; set; } = "0";
        public string RejectedTransactions { get; set; } = "0";
        public string TotalTransactionValue { get; set; } = "0";
        public string TotalRewardsEarned { get; set; } = "0";
        public string TotalPenaltyAmount { get; set; } = "0";
        public string? NetworkEndpointIPv4 { get; set; }
        public string? NetworkEndpointIPv6 { get; set; }
        public string? NetworkEndpointDns { get; set; }
        public double CommissionRate { get; set; }
        public string? Description { get; set; }
        public List<Guid> ConfirmedValidationIds { get; set; } = [];
        public List<Guid> RejectedValidationIds { get; set; } = [];
    }
}