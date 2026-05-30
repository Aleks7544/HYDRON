using System.Net;
using System.Numerics;
using System.Text;

namespace HYDRON.Models
{
    public class Validator : Account
    {
        public Atomos StakedAmount { get; private set; }
        public ValidatorTier Tier { get; private set; }
        public ValidatorStatus Status { get; private set; }

        public BigInteger CorrectVotes { get; private set; }
        public BigInteger TotalVotes { get; private set; }

        public double ReputationScore => TotalVotes == 0
            ? 0.0
            : (double)CorrectVotes / (double)TotalVotes * 100.0;

        public BigInteger TransactionsValidatedCount { get; private set; }
        public BigInteger RejectedTransactionsCount { get; private set; }
        public Atomos TotalTransactionValue { get; private set; }

        private readonly List<Guid> _validationIds = [];
        public IReadOnlyList<Guid> ValidationIds => _validationIds.AsReadOnly();

        public Atomos TotalRewardsEarned { get; private set; }
        public Atomos TotalPenaltyAmount { get; private set; }

        public string? NetworkEndpointIPv4 { get; private set; }
        public string? NetworkEndpointIPv6 { get; private set; }
        public string? NetworkEndpointDns { get; private set; }
        public double CommissionRate { get; private set; }
        public string? Description { get; private set; }

        private const int MaxDescriptionLength = 1000;

        public Validator(
            string address,
            string publicKey,
            string stealthPublicKey,
            Atomos stakedAmount,
            string? networkEndpointIPv4 = null,
            string? networkEndpointIPv6 = null,
            string? networkEndpointDns = null,
            double commissionRate = 0.0,
            string? description = null)
            : base(address, publicKey, stealthPublicKey)
        {
            if (stakedAmount < Atomos.One)
                throw new ArgumentException("Minimum stake is 1 atomos.", nameof(stakedAmount));

            if (string.IsNullOrWhiteSpace(networkEndpointIPv4) &&
                string.IsNullOrWhiteSpace(networkEndpointIPv6) &&
                string.IsNullOrWhiteSpace(networkEndpointDns))
                throw new ArgumentException("At least one network endpoint must be provided.");

            if (commissionRate is < 0.0 or > 100.0)
                throw new ArgumentException("Commission rate must be between 0 and 100.", nameof(commissionRate));

            if (description is not null && Encoding.UTF8.GetByteCount(description) > MaxDescriptionLength)
                throw new ArgumentException($"Description cannot exceed {MaxDescriptionLength} UTF-8 bytes.", nameof(description));

            if (networkEndpointIPv4 is not null && !IPAddress.TryParse(networkEndpointIPv4, out _))
                throw new ArgumentException("Invalid IPv4 address format.", nameof(networkEndpointIPv4));

            if (networkEndpointIPv6 is not null && !IPAddress.TryParse(networkEndpointIPv6, out _))
                throw new ArgumentException("Invalid IPv6 address format.", nameof(networkEndpointIPv6));

            if (networkEndpointDns is not null && !IsValidDnsName(networkEndpointDns))
                throw new ArgumentException("Invalid DNS name format.", nameof(networkEndpointDns));

            StakedAmount = stakedAmount;
            NetworkEndpointIPv4 = networkEndpointIPv4;
            NetworkEndpointIPv6 = networkEndpointIPv6;
            NetworkEndpointDns = networkEndpointDns;
            CommissionRate = commissionRate;
            Description = description;
            Tier = ValidatorTier.Edge;
            Status = ValidatorStatus.Active;
        }

        public void AddStake(Atomos amount)
        {
            StakedAmount += amount;

            if (Status == ValidatorStatus.Penalized && StakedAmount >= Atomos.One)
                Status = ValidatorStatus.Active;
        }

        public void WithdrawStake(Atomos amount)
        {
            if (amount > StakedAmount)
                throw new InvalidOperationException("Cannot withdraw more than staked amount.");

            StakedAmount -= amount;

            if (StakedAmount < Atomos.One)
                Status = ValidatorStatus.Inactive;
        }

        public void RecordVote(bool votedCorrectly)
        {
            TotalVotes++;

            if (votedCorrectly)
                CorrectVotes++;
        }

        public void RecordValidation(Guid validationId, Atomos transactionAmount)
        {
            if (transactionAmount <= Atomos.Zero)
                throw new ArgumentException("Transaction amount must be greater than zero.", nameof(transactionAmount));

            _validationIds.Add(validationId);
            TransactionsValidatedCount++;
            TotalTransactionValue += transactionAmount;
        }

        public void RecordRejection(Guid validationId)
        {
            _validationIds.Add(validationId);
            RejectedTransactionsCount++;
        }

        public void ApplyPenalty(Atomos penaltyAmount, string evidence)
        {
            if (string.IsNullOrWhiteSpace(evidence))
                throw new ArgumentException("Penalty evidence cannot be null or empty.", nameof(evidence));

            if (penaltyAmount > StakedAmount)
                penaltyAmount = StakedAmount;

            StakedAmount -= penaltyAmount;
            TotalPenaltyAmount += penaltyAmount;

            if (StakedAmount < Atomos.One)
                Status = ValidatorStatus.Penalized;
        }

        public void ReceiveReward(Atomos rewardAmount)
        {
            StakedAmount += rewardAmount;
            TotalRewardsEarned += rewardAmount;

            if (Status == ValidatorStatus.Penalized && StakedAmount >= Atomos.One)
                Status = ValidatorStatus.Active;
        }

        public bool IsReachable() =>
            Status != ValidatorStatus.Unreachable &&
            Status != ValidatorStatus.Penalized;

        public void MarkUnreachable()
        {
            if (Status == ValidatorStatus.Active)
                Status = ValidatorStatus.Unreachable;
        }

        internal void RestoreReachable()
        {
            if (Status == ValidatorStatus.Unreachable && StakedAmount >= Atomos.One)
                Status = ValidatorStatus.Active;
        }

        public void UpdateNetworkEndpoints(
            string? networkEndpointIPv4 = null,
            string? networkEndpointIPv6 = null,
            string? networkEndpointDns = null)
        {
            if (networkEndpointIPv4 is null && networkEndpointIPv6 is null && networkEndpointDns is null)
                throw new ArgumentException("At least one network endpoint must be provided.");

            if (networkEndpointIPv4 is not null && !IPAddress.TryParse(networkEndpointIPv4, out _))
                throw new ArgumentException("Invalid IPv4 address format.", nameof(networkEndpointIPv4));

            if (networkEndpointIPv6 is not null && !IPAddress.TryParse(networkEndpointIPv6, out _))
                throw new ArgumentException("Invalid IPv6 address format.", nameof(networkEndpointIPv6));

            if (networkEndpointDns is not null && !IsValidDnsName(networkEndpointDns))
                throw new ArgumentException("Invalid DNS name format.", nameof(networkEndpointDns));

            NetworkEndpointIPv4 = networkEndpointIPv4;
            NetworkEndpointIPv6 = networkEndpointIPv6;
            NetworkEndpointDns = networkEndpointDns;
        }

        public Atomos GetVotingWeight() => StakedAmount;

        private static bool IsValidDnsName(string dns)
        {
            if (string.IsNullOrWhiteSpace(dns) || dns.Length > 253)
                return false;

            string[] labels = dns.Split('.');
            foreach (string label in labels)
            {
                if (label.Length is 0 or > 63)
                    return false;
                if (!label.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                    return false;
                if (label.StartsWith('-') || label.EndsWith('-'))
                    return false;
            }

            return true;
        }

        public override string ToString() =>
            $"VALIDATOR (Address: {Address} | Balance: {Balance} | Stake: {StakedAmount} | Tier: {Tier} | Reputation: {ReputationScore:F2}% | Status: {Status})";
    }
}