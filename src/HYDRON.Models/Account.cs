using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace HYDRON.Models
{
    public class Account
    {
        public string Address { get; }
        public string PublicKey { get; }
        public string StealthPublicKey { get; private set; }
        public string? Handle { get; private set; }

        private Atomos _balance = Atomos.Zero;
        private readonly Lock _balanceLock = new();

        public Atomos Balance
        {
            get { lock (_balanceLock) return _balance; }
        }

        public BigInteger Nonce { get; private set; }

        private readonly Lock _stateHashLock = new();
        private string? _stateHashCache;

        public string StateHash
        {
            get
            {
                _stateHashLock.Enter();
                try
                {
                    return _stateHashCache ??= ComputeStateHash();
                }
                finally
                {
                    _stateHashLock.Exit();
                }
            }
        }

        public Account(string address, string publicKey, string stealthPublicKey)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be null or empty.", nameof(address));
            if (string.IsNullOrWhiteSpace(publicKey))
                throw new ArgumentException("Public key cannot be null or empty.", nameof(publicKey));
            if (string.IsNullOrWhiteSpace(stealthPublicKey))
                throw new ArgumentException("Stealth public key cannot be null or empty.", nameof(stealthPublicKey));

            Address = address;
            PublicKey = publicKey;
            StealthPublicKey = stealthPublicKey;
            Nonce = BigInteger.Zero;
        }

        public bool TryDeductBalance(Atomos amount)
        {
            lock (_balanceLock)
            {
                if (_balance < amount) return false;
                _balance -= amount;
                InvalidateStateHash();
            }
            
            return true;
        }

        public void AddBalance(Atomos amount)
        {
            lock (_balanceLock)
            {
                _balance += amount;
                InvalidateStateHash();
            }
            
        }

        public void IncrementNonce()
        {
            Nonce++;
            InvalidateStateHash();
        }

        private const int MaxHandleLength = 1000;

        public void UpdateHandle(string? newHandle)
        {
            if (newHandle is not null)
            {
                if (string.IsNullOrWhiteSpace(newHandle))
                    throw new ArgumentException("Handle cannot be whitespace-only.", nameof(newHandle));

                int byteLength = Encoding.UTF8.GetByteCount(newHandle);
                if (byteLength > MaxHandleLength)
                    throw new ArgumentException($"Handle cannot exceed {MaxHandleLength} UTF-8 bytes.", nameof(newHandle));
            }
            Handle = newHandle;
            InvalidateStateHash();
        }

        internal void ApplyStealthKeyRotation(string newStealthPublicKey)
        {
            if (string.IsNullOrWhiteSpace(newStealthPublicKey))
                throw new ArgumentException("Stealth public key cannot be null or empty.", nameof(newStealthPublicKey));

            StealthPublicKey = newStealthPublicKey;
            InvalidateStateHash();
        }

        private string ComputeStateHash()
        {
            StringBuilder sb = new();
            sb.Append(Address).Append('|')
              .Append(PublicKey).Append('|')
              .Append(StealthPublicKey).Append('|')
              .Append(Balance).Append('|')
              .Append(Nonce).Append('|')
              .Append(Handle ?? string.Empty);

            AppendExtraHashFields(sb);

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            byte[] hash = SHA256.HashData(bytes);
            return Convert.ToHexStringLower(hash);
        }

        protected virtual void AppendExtraHashFields(StringBuilder sb) { }

        protected void InvalidateStateHash()
        {
            _stateHashLock.Enter();
            try
            {
                _stateHashCache = null;
            }
            finally
            {
                _stateHashLock.Exit();
            }
        }

        public override string ToString() =>
            $"ACCOUNT (Address: {Address} | Balance: {Balance} | Nonce: {Nonce} | Handle: {Handle ?? "N/A"})";
    }
}
