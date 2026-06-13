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

        public Atomos Balance { get; private set; }
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
            Balance = Atomos.Zero;
            Nonce = BigInteger.Zero;
        }

        // ── Balance ───────────────────────────────────────────────────────────

        public bool TryDeductBalance(Atomos amount)
        {
            if (Balance < amount) return false;
            Balance -= amount;
            InvalidateStateHash();
            return true;
        }

        public void AddBalance(Atomos amount)
        {
            Balance += amount;
            InvalidateStateHash();
        }

        // ── Nonce ─────────────────────────────────────────────────────────────

        public void IncrementNonce()
        {
            Nonce++;
            InvalidateStateHash();
        }

        // ── Handle ────────────────────────────────────────────────────────────

        private const int MaxHandleLength = 1000;

        public void UpdateHandle(string? newHandle)
        {
            if (newHandle is not null)
            {
                int byteLength = Encoding.UTF8.GetByteCount(newHandle);
                if (byteLength > MaxHandleLength)
                    throw new ArgumentException($"Handle cannot exceed {MaxHandleLength} UTF-8 bytes.", nameof(newHandle));
            }
            Handle = newHandle;
            InvalidateStateHash();
        }

        // ── Stealth Key Rotation ──────────────────────────────────────────────

        internal void ApplyStealthKeyRotation(string newStealthPublicKey)
        {
            if (string.IsNullOrWhiteSpace(newStealthPublicKey))
                throw new ArgumentException("Stealth public key cannot be null or empty.", nameof(newStealthPublicKey));

            StealthPublicKey = newStealthPublicKey;
            InvalidateStateHash();
        }

        // ── State Hash ────────────────────────────────────────────────────────

        private string ComputeStateHash()
        {
            string raw = $"{Address}|{PublicKey}|{StealthPublicKey}|{Balance}|{Nonce}|{Handle ?? string.Empty}";
            byte[] bytes = Encoding.UTF8.GetBytes(raw);
            byte[] hash = SHA256.HashData(bytes);

            return Convert.ToHexStringLower(hash);
        }

        private void InvalidateStateHash()
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

        // ── Display ───────────────────────────────────────────────────────────

        public override string ToString() =>
            $"ACCOUNT (Address: {Address} | Balance: {Balance} | Nonce: {Nonce} | Handle: {Handle ?? "N/A"})";
    }
}