namespace HYDRON.Models
{
    public enum Denominations
    {
        Hya = 0,
        Hyb = 1,
        Hyg = 2,
        Hyd = 3,
        Hye = 4,
        Hyz = 5
    }

    public enum Priority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    public enum TransactionStatus
    {
        InitiatedBySender,
        AwaitingReceiverAcceptance,
        AbortedBySender,
        AbortedByReceiver,
        TimedOut,
        PendingValidation,
        ConsensusReached,
        Rejected,
        Settled
    }

    public enum ValidationStatus
    {
        Pending,
        Confirmed,
        Rejected
    }

    public enum ValidatorStatus
    {
        Active,
        Inactive,
        Unreachable,
        Penalized
    }

    public enum ValidatorTier
    {
        Core,
        Edge
    }

    public enum RewardStatus
    {
        Pending,
        Settled
    }

    public enum PrivacyMode
    {
        Public,
        HiddenReceiver,
        HiddenSender,
        FullyPrivate
    }
}