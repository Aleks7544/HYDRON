using HYDRON.Models;

namespace HYDRON.Database.Dtos
{
    internal sealed class TransactionDto
    {
        public string Sender { get; set; } = string.Empty;
        public string Receiver { get; set; } = string.Empty;
        public string Amount { get; set; } = "0";
        public string Fee { get; set; } = "0";
        public string Nonce { get; set; } = "0";
        public string SenderSignature { get; set; } = string.Empty;
        public string? ReceiverSignature { get; set; }
        public string Hash { get; set; } = string.Empty;
        public TransactionStatus Status { get; set; }
        public bool RequiresReceiverConfirmation { get; set; }
        public DateTimeOffset InitiatedAt { get; set; }
        public bool IsFinalized { get; set; }
        public DateTimeOffset? FinalizedAt { get; set; }
        public Priority? Priority { get; set; }
        public string? TransactionBlockNumber { get; set; }
        public PrivacyMode PrivacyMode { get; set; }
        public string? EphemeralPublicKey { get; set; }
        public List<string> AssignedValidators { get; set; } = [];
    }
}