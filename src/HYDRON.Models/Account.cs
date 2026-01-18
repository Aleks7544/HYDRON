using System.Numerics;

namespace Hydron.Models
{
    public enum AccountType
    {
        Regular,
        Validator
    }

    public class Account
    {
        public string Address { get; set; }
        public Atomos Balance { get; set; }
        public ulong Nonce { get; set; }
        public AccountType Type { get; set; }
        public string PublicKey { get; set; }
        public string StateHash { get; set; }

        public Account(string address, AccountType type, string publicKey)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be null or empty.", nameof(address));
            if (string.IsNullOrWhiteSpace(publicKey))
                throw new ArgumentException("Public key cannot be null or empty.", nameof(publicKey));

            Address = address;
            Balance = new Atomos(new BigInteger(0));
            Nonce = 0;
            Type = type;
            PublicKey = publicKey;
            StateHash = string.Empty;
        }

        public void IncrementNonce() => Nonce++;

        public bool TryDeductBalance(Atomos amount)
        {
            if (Balance < amount)
                return false;

            Balance -= amount;
            return true;
        }

        public void AddBalance(Atomos amount) => Balance += amount;

        public void UpdateStateHash(string newHash)
        {
            if (string.IsNullOrWhiteSpace(newHash))
                throw new ArgumentException("State hash cannot be null or empty.", nameof(newHash));
            StateHash = newHash;
        }

        public override string ToString() => $"Account({Address}, Type: {Type}, Balance: {Balance.ToString()}, Nonce: {Nonce})";
    }
}
