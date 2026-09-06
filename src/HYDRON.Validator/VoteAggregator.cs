using HYDRON.Core;

namespace HYDRON.Validator
{
    public class VoteAggregator
    {
        private readonly string _firstValidatorAddress;
        private readonly int _totalAssigned;

        private readonly List<string> _approvers = [];
        private readonly List<string> _rejecters = [];
        private readonly HashSet<string> _voted = new(StringComparer.OrdinalIgnoreCase);

        public ConsensusResult Result { get; private set; } = ConsensusResult.Pending;

        public IReadOnlyList<string> Approvers => _approvers.AsReadOnly();
        public IReadOnlyList<string> Rejecters => _rejecters.AsReadOnly();

        public int VotesCast => _approvers.Count + _rejecters.Count;
        public bool AllVoted => VotesCast >= _totalAssigned;

        public VoteAggregator(string firstValidatorAddress, int totalAssigned)
        {
            if (string.IsNullOrWhiteSpace(firstValidatorAddress))
                throw new ArgumentException(
                    "First validator address cannot be null or empty.",
                    nameof(firstValidatorAddress));
            if (totalAssigned <= 0)
                throw new ArgumentException(
                    "Total assigned validators must be greater than zero.",
                    nameof(totalAssigned));

            _firstValidatorAddress = firstValidatorAddress;
            _totalAssigned = totalAssigned;
        }

        public void SubmitVote(string validatorAddress, bool isApproval)
        {
            if (string.IsNullOrWhiteSpace(validatorAddress))
                throw new ArgumentException(
                    "Validator address cannot be null or empty.",
                    nameof(validatorAddress));
            if (Result != ConsensusResult.Pending)
                throw new InvalidOperationException(
                    $"Cannot submit a vote after consensus has been reached ({Result}).");
            if (!_voted.Add(validatorAddress))
                throw new InvalidOperationException(
                    $"Validator {validatorAddress} has already voted.");

            if (isApproval)
                _approvers.Add(validatorAddress);
            else
                _rejecters.Add(validatorAddress);

            if (!isApproval &&
                string.Equals(validatorAddress, _firstValidatorAddress,
                    StringComparison.OrdinalIgnoreCase))
            {
                Result = ConsensusResult.VetoedByFirstValidator;
                return;
            }

            if (SystemConstants.IsSupermajority(_approvers.Count, _totalAssigned))
            {
                Result = ConsensusResult.Approved;
                return;
            }

            if (!SystemConstants.IsSupermajority(_rejecters.Count, _totalAssigned)) return;

            Result = ConsensusResult.Rejected;
        }
    }
}