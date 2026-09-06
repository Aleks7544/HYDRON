using HYDRON.Models;

namespace HYDRON.Validator
{
    public class ConsensusService
    {
        private readonly Transaction _tx;
        private readonly Dictionary<string, Models.Validator> _validatorMap;
        private readonly VoteAggregator _aggregator;

        public ConsensusResult Result => _aggregator.Result;
        public IReadOnlyList<string> Approvers => _aggregator.Approvers;
        public IReadOnlyList<string> Rejecters => _aggregator.Rejecters;

        public ConsensusService(
            Transaction tx,
            IReadOnlyList<Models.Validator> assignedValidators)
        {
            ArgumentNullException.ThrowIfNull(tx);
            ArgumentNullException.ThrowIfNull(assignedValidators);

            if (assignedValidators.Count == 0)
                throw new ArgumentException(
                    "Assigned validators list cannot be empty.",
                    nameof(assignedValidators));
            if (tx.Status != TransactionStatus.PendingValidation)
                throw new ArgumentException(
                    $"Transaction must be in {TransactionStatus.PendingValidation} status. " +
                    $"Current: {tx.Status}.", nameof(tx));

            _tx = tx;
            _validatorMap = assignedValidators.ToDictionary(
                v => v.Address, StringComparer.OrdinalIgnoreCase);

            string firstValidator = tx.AssignedValidators[0];
            _aggregator = new VoteAggregator(firstValidator, assignedValidators.Count);
        }

        public void SubmitVote(
            string validatorAddress,
            bool isApproval,
            string signature,
            double speedMs)
        {
            if (string.IsNullOrWhiteSpace(validatorAddress))
                throw new ArgumentException(
                    "Validator address cannot be null or empty.",
                    nameof(validatorAddress));
            if (string.IsNullOrWhiteSpace(signature))
                throw new ArgumentException(
                    "Signature cannot be null or empty.", nameof(signature));
            if (speedMs < 0)
                throw new ArgumentException(
                    "Speed cannot be negative.", nameof(speedMs));
            if (!_validatorMap.ContainsKey(validatorAddress))
                throw new InvalidOperationException(
                    $"Validator {validatorAddress} is not assigned to this transaction.");
            if (Result != ConsensusResult.Pending)
                throw new InvalidOperationException(
                    $"Cannot submit a vote after consensus has been reached ({Result}).");

            Validation validation = new Validation(_tx.Hash, validatorAddress);
            validation.SignValidation(signature);

            if (isApproval)
                validation.Confirm(speedMs);
            else
                validation.Reject(speedMs);

            _tx.AddValidation(validation);
            _aggregator.SubmitVote(validatorAddress, isApproval);
        }

        public bool TryFinalize()
        {
            switch (Result)
            {
                case ConsensusResult.Approved:
                    _tx.UpdateStatus(TransactionStatus.ConsensusReached);
                    return true;

                case ConsensusResult.Rejected:
                case ConsensusResult.VetoedByFirstValidator:
                    _tx.UpdateStatus(TransactionStatus.Rejected);
                    return true;

                default:
                    return false;
            }
        }
    }
}