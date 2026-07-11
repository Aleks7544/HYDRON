namespace HYDRON.Models
{
    public class Validation
    {
        public Guid Id { get; private set; }

        public string TransactionHash { get; private set; }
        public string ValidatorAddress { get; private set; }

        public ValidationStatus Status { get; private set; } = ValidationStatus.Pending;
        public string? ValidationSignature { get; private set; }

        public double? ValidationSpeedMs { get; private set; }
        public DateTimeOffset? ValidatedAt { get; private set; }

        public Atomos? FeeReward { get; private set; }

        public bool IsPenalized => PenaltyAmount is not null;
        public Atomos? PenaltyAmount { get; private set; }
        public string? PenaltyEvidence { get; private set; }
        public DateTimeOffset? PenaltyTimestamp { get; private set; }

        public Validation(string transactionHash, string validatorAddress)
        {
            if (string.IsNullOrWhiteSpace(transactionHash))
                throw new ArgumentException("Transaction hash cannot be null or empty.", nameof(transactionHash));
            if (string.IsNullOrWhiteSpace(validatorAddress))
                throw new ArgumentException("Validator address cannot be null or empty.", nameof(validatorAddress));

            Id = Guid.CreateVersion7(DateTimeOffset.UtcNow);
            TransactionHash = transactionHash;
            ValidatorAddress = validatorAddress;
        }

        public void SignValidation(string signature)
        {
            if (Status != ValidationStatus.Pending)
                throw new InvalidOperationException("Cannot sign a validation that has already been completed.");
            if (ValidationSignature is not null)
                throw new InvalidOperationException("Validation has already been signed.");
            if (string.IsNullOrWhiteSpace(signature))
                throw new ArgumentException("Signature cannot be empty.", nameof(signature));

            ValidationSignature = signature;
        }

        public void Confirm(double validationSpeedMs)
        {
            if (ValidationSignature is null)
                throw new InvalidOperationException("Validation must be signed before it can be confirmed.");
            if (Status != ValidationStatus.Pending)
                throw new InvalidOperationException($"Cannot confirm validation with status {Status}.");
            if (ValidationSignature is null)
                throw new InvalidOperationException("Validation must be signed before it can be confirmed.");
            if (validationSpeedMs < 0)
                throw new ArgumentException("Validation speed cannot be negative.", nameof(validationSpeedMs));

            Status = ValidationStatus.Confirmed;
            ValidationSpeedMs = validationSpeedMs;
            ValidatedAt = DateTimeOffset.UtcNow;
        }

        public void Reject(double validationSpeedMs)
        {
            if (ValidationSignature is null)
                throw new InvalidOperationException("Validation must be signed before it can be rejected.");
            if (Status != ValidationStatus.Pending)
                throw new InvalidOperationException($"Cannot reject validation with status {Status}.");
            if (ValidationSignature is null)
                throw new InvalidOperationException("Validation must be signed before it can be rejected.");
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
            if (Status != ValidationStatus.Confirmed)
                throw new InvalidOperationException("Only confirmed validations can be penalized.");
            if (IsPenalized)
                throw new InvalidOperationException("Validation already penalized.");
            if (penaltyAmount <= Atomos.Zero)
                throw new ArgumentException("Penalty amount must be greater than zero.", nameof(penaltyAmount));
            if (string.IsNullOrWhiteSpace(evidence))
                throw new ArgumentException("Penalty evidence cannot be null or empty.", nameof(evidence));

            PenaltyAmount = penaltyAmount;
            PenaltyEvidence = evidence;
            PenaltyTimestamp = DateTimeOffset.UtcNow;
        }

        public override string ToString() =>
            $"VALIDATION (Id: {Id} | Tx: {TransactionHash} | Validator: {ValidatorAddress} | Status: {Status} | Penalized: {IsPenalized})";
    }
}
