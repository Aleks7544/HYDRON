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
        Low = 0,
        Medium = 1,
        High = 2,
        Urgent = 3
    }

    public enum TransactionStatus
    {
        InitiatedBySender = 0,
        AwaitingReceiverAcceptance = 1,
        PendingValidation = 2,
        AbortedBySender = 3,
        AbortedByReceiver = 4,
        TimedOut = 5,
        ConsensusReached = 6,
        Rejected = 7,
        Settled = 8
    }

    public enum ValidationStatus
    {
        Pending = 0,
        Confirmed = 1,
        Rejected = 2
    }

    public enum ValidatorStatus
    {
        Active = 0,
        Inactive = 1,
        Unreachable = 2,
        Penalized = 3,
        Warned = 4,
        Suspended = 5
    }

    public enum ValidatorTier
    {
        Core = 0,
        Edge = 1
    }

    public enum RewardStatus
    {
        Pending = 0,
        Settled = 1
    }

    public enum PrivacyMode
    {
        Public = 0,
        HiddenReceiver = 1,
        FullyPrivate = 2
    }
}