namespace Hydron.Models
{
    public class Transaction
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public Atomos Amount { get; set; }
        public ulong Nonce { get; set; }
        public Atomos Fee { get; set; }
        public string Signature { get; set; }
        public DateTime Timestamp { get; set; }
        public string Hash { get; set; }

        public Transaction(string sender, string receiver, Atomos amount, ulong nonce, Atomos fee)
        {
            if (string.IsNullOrWhiteSpace(sender))
                throw new ArgumentException("Sender cannot be null or empty.", nameof(sender));
            if (string.IsNullOrWhiteSpace(receiver))
                throw new ArgumentException("Receiver cannot be null or empty.", nameof(receiver));
            if (sender == receiver)
                throw new ArgumentException("Sender and receiver cannot be the same.", nameof(receiver));

            Sender = sender;
            Receiver = receiver;
            Amount = amount;
            Nonce = nonce;
            Fee = fee;
            Signature = string.Empty;
            Timestamp = DateTime.UtcNow;
            Hash = string.Empty;
        }

        public Atomos GetTotalCost() => Amount + Fee;

        public bool IsSigned() => !string.IsNullOrEmpty(Signature);

        public void SetSignature(string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
                throw new ArgumentException("Signature cannot be null or empty.", nameof(signature));

            Signature = signature;
        }

        public void SetHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Hash cannot be null or empty.", nameof(hash));

            Hash = hash;
        }

        public override string ToString() => $"TX:({Sender} -> {Receiver} | {Amount.ToString()} | Fee: {Fee.ToString()} | Nonce: {Nonce})";
    }
}
