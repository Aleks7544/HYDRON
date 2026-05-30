namespace HYDRON.Models
{
    public class Validation
    {
        public Guid Id { get; private set; }

        public string TransactionHash { get; private set; }
        public Transaction Transaction { get; private set; }

        public string ValidatorAddress { get; private set; }

        public ValidationStatus Status { get; private set; } = ValidationStatus.Pending;
        public string? ValidationSignature { get; private set; }

        public double? ValidationSpeedMs { get; private set; }
        public DateTimeOffset? ValidatedAt { get; private set; }

        public Atomos? FeeReward { get; private set; }

        public bool IsPenalized { get; private set; }
        public Atomos? PenaltyAmount { get; private set; }
        public string? PenaltyEvidence { get; private set; }
        public DateTimeOffset? PenaltyTimestamp { get; private set; }

        public Validation(Transaction transaction, string validatorAddress)
        {
            ArgumentNullException.ThrowIfNull(transaction);

            if (string.IsNullOrWhiteSpace(validatorAddress))
                throw new ArgumentException("Validator address cannot be null or empty.", nameof(validatorAddress));

            Id = Guid.CreateVersion7(DateTimeOffset.UtcNow);
            TransactionHash = transaction.Hash;
            Transaction = transaction;
            ValidatorAddress = validatorAddress;
        }

        public void SignValidation(string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
                throw new ArgumentException("Signature cannot be empty.", nameof(signature));

            ValidationSignature = signature;
        }

        public void Confirm(double validationSpeedMs)
        {
            if (Status != ValidationStatus.Pending)
                throw new InvalidOperationException(
                    $"Cannot confirm validation with status {Status}.");

            if (validationSpeedMs < 0)
                throw new ArgumentException("Validation speed cannot be negative.", nameof(validationSpeedMs));

            Status = ValidationStatus.Confirmed;
            ValidationSpeedMs = validationSpeedMs;
            ValidatedAt = DateTimeOffset.UtcNow;
        }

        public void Reject(double validationSpeedMs)
        {
            if (Status != ValidationStatus.Pending)
                throw new InvalidOperationException(
                    $"Cannot reject validation with status {Status}.");

            if (validationSpeedMs < 0)
                throw new ArgumentException("Validation speed cannot be negative.", nameof(validationSpeedMs));

            Status = ValidationStatus.Rejected;
            ValidationSpeedMs = validationSpeedMs;
            ValidatedAt = DateTimeOffset.UtcNow;
        }

        public void AssignReward(Atomos reward)
        {
            if (Status != ValidationStatus.Confirmed)
                throw new InvalidOperationException("Reward can only be assigned to a confirmed validation.");

            if (FeeReward is not null)
                throw new InvalidOperationException("Reward has already been assigned.");

            FeeReward = reward;
        }

        public void Penalize(Atomos penaltyAmount, string evidence)
        {
            if (IsPenalized)
                throw new InvalidOperationException("Validation already penalized.");

            if (string.IsNullOrWhiteSpace(evidence))
                throw new ArgumentException("Penalty evidence cannot be null or empty.", nameof(evidence));

            IsPenalized = true;
            PenaltyAmount = penaltyAmount;
            PenaltyEvidence = evidence;
            PenaltyTimestamp = DateTimeOffset.UtcNow;
        }

        public override string ToString() =>
            $"VALIDATION (Id: {Id} | Tx: {TransactionHash} | Validator: {ValidatorAddress} | Status: {Status} | Penalized: {IsPenalized})";
    }
}