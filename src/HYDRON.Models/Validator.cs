using System.Net;
using System.Net.Sockets;
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
            : Math.Min((double)CorrectVotes / (double)TotalVotes * 100.0, 100.0);

        public BigInteger TransactionsValidatedCount { get; private set; }
        public BigInteger RejectedTransactionsCount { get; private set; }
        public Atomos TotalTransactionValue { get; private set; }

        private readonly HashSet<Guid> _validationIds = [];
        public IReadOnlySet<Guid> ValidationIds => _validationIds;

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

            ValidateIPv4(networkEndpointIPv4, nameof(networkEndpointIPv4));
            ValidateIPv6(networkEndpointIPv6, nameof(networkEndpointIPv6));

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
            if (amount <= Atomos.Zero)
                throw new ArgumentException("Stake amount must be greater than zero.", nameof(amount));

            StakedAmount += amount;
            InvalidateStateHash();

            if (Status is ValidatorStatus.Penalized or ValidatorStatus.Inactive
                && StakedAmount >= Atomos.One)
                Status = ValidatorStatus.Active;
        }

        public void WithdrawStake(Atomos amount)
        {
            if (amount <= Atomos.Zero)
                throw new ArgumentException("Withdrawal amount must be greater than zero.", nameof(amount));
            if (Status == ValidatorStatus.Penalized)
                throw new InvalidOperationException("Penalized validators cannot withdraw stake.");
            if (amount > StakedAmount)
                throw new InvalidOperationException("Cannot withdraw more than staked amount.");

            StakedAmount -= amount;
            InvalidateStateHash();

            if (StakedAmount < Atomos.One)
                Status = ValidatorStatus.Inactive;
        }

        public void RecordVote(bool votedCorrectly)
        {
            TotalVotes++;
            if (votedCorrectly) CorrectVotes++;
        }

        public void RecordValidation(Guid validationId, Atomos transactionAmount)
        {
            if (transactionAmount <= Atomos.Zero)
                throw new ArgumentException("Transaction amount must be greater than zero.", nameof(transactionAmount));
            if (!_validationIds.Add(validationId))
                throw new InvalidOperationException($"Validation {validationId} has already been recorded.");

            TransactionsValidatedCount++;
            TotalTransactionValue += transactionAmount;
        }

        public void RecordRejection(Guid validationId)
        {
            if (!_validationIds.Add(validationId))
                throw new InvalidOperationException($"Validation {validationId} has already been recorded.");

            RejectedTransactionsCount++;
        }

        public void ApplyPenalty(Atomos requestedPenaltyAmount, string evidence)
        {
            if (requestedPenaltyAmount <= Atomos.Zero)
                throw new ArgumentException("Penalty amount must be greater than zero.", nameof(requestedPenaltyAmount));
            if (string.IsNullOrWhiteSpace(evidence))
                throw new ArgumentException("Penalty evidence cannot be null or empty.", nameof(evidence));

            Atomos actualPenalty = requestedPenaltyAmount > StakedAmount
                ? StakedAmount
                : requestedPenaltyAmount;

            StakedAmount -= actualPenalty;
            TotalPenaltyAmount += actualPenalty;
            InvalidateStateHash();

            if (StakedAmount < Atomos.One)
                Status = ValidatorStatus.Penalized;
        }

        public void ReceiveReward(Atomos rewardAmount)
        {
            if (rewardAmount <= Atomos.Zero)
                throw new ArgumentException("Reward amount must be greater than zero.", nameof(rewardAmount));

            StakedAmount += rewardAmount;
            TotalRewardsEarned += rewardAmount;
            InvalidateStateHash();

            if (Status is ValidatorStatus.Penalized or ValidatorStatus.Inactive
                && StakedAmount >= Atomos.One)
                Status = ValidatorStatus.Active;
        }

        public void Warn()
        {
            if (Status == ValidatorStatus.Active)
                Status = ValidatorStatus.Warned;
        }

        public void Suspend()
        {
            if (Status is ValidatorStatus.Active or ValidatorStatus.Warned)
                Status = ValidatorStatus.Suspended;
        }

        public bool IsReachable() =>
            Status != ValidatorStatus.Unreachable &&
            Status != ValidatorStatus.Penalized &&
            Status != ValidatorStatus.Suspended;

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

            ValidateIPv4(networkEndpointIPv4, nameof(networkEndpointIPv4));
            ValidateIPv6(networkEndpointIPv6, nameof(networkEndpointIPv6));

            if (networkEndpointDns is not null && !IsValidDnsName(networkEndpointDns))
                throw new ArgumentException("Invalid DNS name format.", nameof(networkEndpointDns));

            NetworkEndpointIPv4 = networkEndpointIPv4 ?? NetworkEndpointIPv4;
            NetworkEndpointIPv6 = networkEndpointIPv6 ?? NetworkEndpointIPv6;
            NetworkEndpointDns = networkEndpointDns ?? NetworkEndpointDns;
        }

        public Atomos GetVotingWeight() =>
            Status is ValidatorStatus.Penalized or ValidatorStatus.Suspended
                ? Atomos.Zero
                : StakedAmount;

        public void UpdateTier(ValidatorTier tier)
        {
            Tier = tier;
            InvalidateStateHash();
        }

        protected override void AppendExtraHashFields(StringBuilder sb)
        {
            sb.Append('|').Append(StakedAmount)
              .Append('|').Append(Tier)
              .Append('|').Append(Status);
        }

        private static void ValidateIPv4(string? address, string paramName)
        {
            if (address is null) return;
            if (!IPAddress.TryParse(address, out IPAddress? parsed) ||
                parsed.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("Invalid IPv4 address format.", paramName);
        }

        private static void ValidateIPv6(string? address, string paramName)
        {
            if (address is null) return;
            if (!IPAddress.TryParse(address, out IPAddress? parsed) ||
                parsed.AddressFamily != AddressFamily.InterNetworkV6)
                throw new ArgumentException("Invalid IPv6 address format.", paramName);
        }

        private static bool IsValidDnsName(string dns)
        {
            if (string.IsNullOrWhiteSpace(dns) || dns.Length > 253)
                return false;

            string[] labels = dns.Split('.');
            foreach (string label in labels)
            {
                if (label.Length is 0 or > 63) return false;
                if (!label.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')) return false;
                if (label.StartsWith('-') || label.EndsWith('-')) return false;
            }

            return true;
        }

        public override string ToString() =>
            $"VALIDATOR (Address: {Address} | Balance: {Balance} | Stake: {StakedAmount} | Tier: {Tier} | Reputation: {ReputationScore:F2}% | Status: {Status})";
    }
}
