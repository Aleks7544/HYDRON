using System.Numerics;

namespace HYDRON.Models
{
    public class Transaction
    {
        public string Sender { get; private set; }
        public string Receiver { get; private set; }
        public Atomos Amount { get; private set; }
        public Atomos Fee { get; private set; }
        public BigInteger Nonce { get; private set; }
        public string SenderSignature { get; private set; }
        public string? ReceiverSignature { get; private set; }
        public string Hash { get; private set; } = string.Empty;

        public TransactionStatus Status { get; private set; } = TransactionStatus.InitiatedBySender;
        public bool RequiresReceiverConfirmation { get; private set; }
        public DateTimeOffset InitiatedAt { get; private set; }
        public bool IsFinalized { get; private set; }
        public DateTimeOffset? FinalizedAt { get; private set; }
        public Priority? Priority { get; private set; }
        public BigInteger? TransactionBlockNumber { get; private set; }

        public PrivacyMode PrivacyMode { get; private set; }
        public string? EphemeralPublicKey { get; private set; }
        public string? FirstValidator => _assignedValidators.Count > 0 ? _assignedValidators[0] : null;
        private int? _frozenValidatorCount;

        private readonly List<string> _assignedValidators = [];
        private readonly List<string> _unassignedValidators = [];
        private readonly List<Guid> _registeredValidationIds = [];
        private readonly List<Guid> _unregisteredValidationIds = [];
        private readonly HashSet<string> _validatingAddresses = [];

        public IReadOnlyList<string> AssignedValidators => _assignedValidators.AsReadOnly();
        public IReadOnlyList<string> UnassignedValidators => _unassignedValidators.AsReadOnly();
        public IReadOnlyList<Guid> RegisteredValidationIds => _registeredValidationIds.AsReadOnly();
        public IReadOnlyList<Guid> UnregisteredValidationIds => _unregisteredValidationIds.AsReadOnly();

        public int RequiredSupermajorityValidationsCount =>
            (int)Math.Ceiling((_frozenValidatorCount ?? _assignedValidators.Count) * 2.0 / 3.0);

        private static readonly Dictionary<TransactionStatus, HashSet<TransactionStatus>> ValidTransitions = new()
        {
            [TransactionStatus.InitiatedBySender] = [
                TransactionStatus.AwaitingReceiverAcceptance,
                TransactionStatus.AbortedBySender,
                TransactionStatus.PendingValidation
            ],
            [TransactionStatus.AwaitingReceiverAcceptance] = [
                TransactionStatus.PendingValidation,
                TransactionStatus.AbortedBySender,
                TransactionStatus.AbortedByReceiver,
                TransactionStatus.TimedOut
            ],
            [TransactionStatus.PendingValidation] = [
                TransactionStatus.ConsensusReached,
                TransactionStatus.Rejected
            ],
            [TransactionStatus.ConsensusReached] = [
                TransactionStatus.Settled
            ],
            [TransactionStatus.AbortedBySender] = [],
            [TransactionStatus.AbortedByReceiver] = [],
            [TransactionStatus.TimedOut] = [],
            [TransactionStatus.Rejected] = [],
            [TransactionStatus.Settled] = [],
        };

        public Transaction(
            string sender,
            string receiver,
            Atomos amount,
            Atomos fee,
            BigInteger nonce,
            string senderSignature,
            bool requiresReceiverConfirmation,
            Priority? priority = null,
            PrivacyMode privacyMode = PrivacyMode.Public,
            string? ephemeralPublicKey = null)
        {
            if (string.IsNullOrWhiteSpace(sender))
                throw new ArgumentException("Sender address cannot be null or empty.", nameof(sender));
            if (string.IsNullOrWhiteSpace(receiver))
                throw new ArgumentException("Receiver address cannot be null or empty.", nameof(receiver));
            if (sender == receiver)
                throw new ArgumentException("Sender and receiver cannot be the same.", nameof(receiver));
            if (string.IsNullOrWhiteSpace(senderSignature))
                throw new ArgumentException("Sender signature cannot be null or empty.", nameof(senderSignature));
            if (amount <= Atomos.Zero)
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
            if (fee < Atomos.Zero)
                throw new ArgumentException("Fee cannot be negative.", nameof(fee));
            if (nonce < BigInteger.Zero)
                throw new ArgumentException("Nonce cannot be negative.", nameof(nonce));
            if (privacyMode != PrivacyMode.Public && string.IsNullOrWhiteSpace(ephemeralPublicKey))
                throw new ArgumentException("Ephemeral public key is required for private transactions.", nameof(ephemeralPublicKey));

            Sender = sender;
            Receiver = receiver;
            Amount = amount;
            Fee = fee;
            Nonce = nonce;
            SenderSignature = senderSignature;
            RequiresReceiverConfirmation = requiresReceiverConfirmation;
            Priority = priority;
            PrivacyMode = privacyMode;
            EphemeralPublicKey = ephemeralPublicKey;
            InitiatedAt = DateTimeOffset.UtcNow;
        }

        public Atomos GetTotalCost() => Amount + Fee;

        public bool IsSignedByReceiver() =>
            !RequiresReceiverConfirmation || !string.IsNullOrEmpty(ReceiverSignature);

        public void SetReceiverSignature(string signature)
        {
            if (!RequiresReceiverConfirmation)
                throw new InvalidOperationException("This transaction does not require receiver confirmation.");
            if (string.IsNullOrWhiteSpace(signature))
                throw new ArgumentException("Signature cannot be null or empty.", nameof(signature));
            if (IsFinalized)
                throw new InvalidOperationException("Cannot set receiver signature on a finalized transaction.");
            if (ReceiverSignature is not null)
                throw new InvalidOperationException("Receiver signature is already set.");

            ReceiverSignature = signature;
        }

        public void SetHash(string hash)
        {
            if (!string.IsNullOrEmpty(Hash))
                throw new InvalidOperationException("Transaction hash has already been set and cannot be changed.");
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Hash cannot be null or empty.", nameof(hash));
            if (IsFinalized)
                throw new InvalidOperationException("Cannot set hash on a finalized transaction.");

            Hash = hash;
        }

        public void UpdateStatus(TransactionStatus newStatus)
        {
            if (IsFinalized)
                throw new InvalidOperationException("Cannot update status of a finalized transaction.");

            if (!ValidTransitions.TryGetValue(Status, out HashSet<TransactionStatus>? allowed) ||
                !allowed.Contains(newStatus))
                throw new InvalidOperationException($"Invalid status transition from {Status} to {newStatus}.");

            if (newStatus == TransactionStatus.PendingValidation)
                _frozenValidatorCount = _assignedValidators.Count;

            Status = newStatus;
        }

        public void AssignBlockNumber(BigInteger blockNumber)
        {
            if (IsFinalized)
                throw new InvalidOperationException("Cannot assign a block number to a finalized transaction.");
            if (blockNumber < BigInteger.Zero)
                throw new ArgumentException("Block number cannot be negative.", nameof(blockNumber));

            TransactionBlockNumber = blockNumber;
        }

        public void AssignValidators(IEnumerable<string> validatorAddresses)
        {
            if (IsFinalized)
                throw new InvalidOperationException("Cannot assign validators to a finalized transaction.");

            foreach (string address in validatorAddresses)
                AddValidator(address);
        }

        public void AddValidator(string validatorAddress)
        {
            if (IsFinalized)
                throw new InvalidOperationException("Cannot add a validator to a finalized transaction.");
            if (string.IsNullOrWhiteSpace(validatorAddress))
                throw new ArgumentException("Validator address cannot be null or empty.", nameof(validatorAddress));
            if (_assignedValidators.Contains(validatorAddress))
                throw new InvalidOperationException($"Validator {validatorAddress} is already assigned to this transaction.");

            _assignedValidators.Add(validatorAddress);
        }

        public void RemoveValidator(string validatorAddress)
        {
            if (IsFinalized)
                throw new InvalidOperationException("Cannot remove a validator from a finalized transaction.");
            if (string.IsNullOrWhiteSpace(validatorAddress))
                throw new ArgumentException("Validator address cannot be null or empty.", nameof(validatorAddress));

            if (!_assignedValidators.Remove(validatorAddress))
                throw new InvalidOperationException($"Validator {validatorAddress} is not assigned to this transaction.");
        }

        public void AddValidation(Validation validation)
        {
            if (IsFinalized)
                throw new InvalidOperationException("Cannot add a validation to a finalized transaction.");

            ArgumentNullException.ThrowIfNull(validation);

            if (!_validatingAddresses.Add(validation.ValidatorAddress))
                throw new InvalidOperationException($"Validator {validation.ValidatorAddress} has already submitted a validation for this transaction.");

            if (_assignedValidators.Contains(validation.ValidatorAddress))
            {
                _registeredValidationIds.Add(validation.Id);
            }
            else
            {
                _unassignedValidators.Add(validation.ValidatorAddress);
                _unregisteredValidationIds.Add(validation.Id);
            }
        }

        public void ChangePriority(Priority newPriority)
        {
            if (IsFinalized)
                throw new InvalidOperationException("Cannot change priority of a finalized transaction.");

            Priority = newPriority;
        }

        public void FinalizeTransaction()
        {
            if (IsFinalized)
                throw new InvalidOperationException("Transaction is already finalized.");

            IsFinalized = true;
            FinalizedAt = DateTimeOffset.UtcNow;
        }

        public override string ToString() =>
            $"TRANSACTION (From: {Sender} → To: {Receiver} | Amount: {Amount} | Fee: {Fee} | Nonce: {Nonce} | Priority: {Priority?.ToString() ?? "None"} | Privacy: {PrivacyMode} | Status: {Status} | InitiatedAt: {InitiatedAt} | IsFinalized: {IsFinalized} | FinalizedAt: {FinalizedAt} | Hash: {Hash} | Block: {TransactionBlockNumber?.ToString() ?? "Unassigned"})";
    }
}