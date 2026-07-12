using NSec.Cryptography;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace HYDRON.Models
{
    public sealed class KeySafe : IDisposable
    {
        private static readonly SignatureAlgorithm Ed25519 = SignatureAlgorithm.Ed25519;
        private static readonly KeyAgreementAlgorithm X25519 = KeyAgreementAlgorithm.X25519;

        private const string HdHmacKey = "HYDRON";

        private readonly byte[] _ed25519PrivateKey;
        private Key? _x25519Key;
        private readonly byte[] _hdChainCode;
        private readonly byte[]? _hdMasterSeed;

        private int _disposed;
        private bool IsDisposed => _disposed == 1;

        public string PublicKey { get; }
        public string? StealthPublicKey { get; private set; }
        public string Address { get; }
        public bool IsStealthSubAccount { get; }

        public KeySafe()
        {
            _hdMasterSeed = GenerateRandomBytes(32);
            (_ed25519PrivateKey, PublicKey) = GenerateEd25519KeyPair();
            (_x25519Key, StealthPublicKey) = GenerateX25519Key();
            _hdChainCode = DeriveHdMasterChainCode(_hdMasterSeed);
            Address = DeriveAddress(PublicKey);
            IsStealthSubAccount = false;
        }

        private KeySafe(
            byte[] ed25519PrivateKey,
            byte[] x25519PrivateKeyBytes,
            byte[] hdMasterSeed)
        {
            _ed25519PrivateKey = ed25519PrivateKey;
            _x25519Key = Key.Import(X25519, x25519PrivateKeyBytes, KeyBlobFormat.RawPrivateKey,
                new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
            _hdMasterSeed = hdMasterSeed;
            _hdChainCode = DeriveHdMasterChainCode(hdMasterSeed);

            PublicKey = DeriveEd25519PublicKey(ed25519PrivateKey);
            StealthPublicKey = Convert.ToBase64String(_x25519Key.PublicKey.Export(KeyBlobFormat.RawPublicKey));
            Address = DeriveAddress(PublicKey);
            IsStealthSubAccount = false;
        }

        private KeySafe(byte[] ed25519PrivateKey, byte[] hdChainCode, bool isStealthSubAccount)
        {
            if (!isStealthSubAccount)
                throw new ArgumentException("Use the public constructor for full KeySafe creation.");

            _ed25519PrivateKey = ed25519PrivateKey;
            _hdChainCode = hdChainCode;
            _x25519Key = null;

            PublicKey = DeriveEd25519PublicKey(ed25519PrivateKey);
            StealthPublicKey = null;
            Address = DeriveAddress(PublicKey);
            IsStealthSubAccount = true;
        }

        public string Sign(string canonicalData)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (string.IsNullOrEmpty(canonicalData))
                throw new ArgumentException("Canonical data cannot be null or empty.", nameof(canonicalData));

            byte[] dataBytes = Encoding.UTF8.GetBytes(canonicalData);
            using Key key = Key.Import(Ed25519, _ed25519PrivateKey, KeyBlobFormat.RawPrivateKey);
            byte[] signature = Ed25519.Sign(key, dataBytes);
            return Convert.ToBase64String(signature);
        }

        public static bool Verify(string canonicalData, string signature, string publicKey)
        {
            if (string.IsNullOrEmpty(canonicalData))
                throw new ArgumentException("Canonical data cannot be null or empty.", nameof(canonicalData));
            if (string.IsNullOrEmpty(signature))
                throw new ArgumentException("Signature cannot be null or empty.", nameof(signature));
            if (string.IsNullOrEmpty(publicKey))
                throw new ArgumentException("Public key cannot be null or empty.", nameof(publicKey));

            try
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(canonicalData);
                byte[] signatureBytes = Convert.FromBase64String(signature);
                byte[] publicKeyBytes = Convert.FromBase64String(publicKey);

                NSec.Cryptography.PublicKey pubKey = NSec.Cryptography.PublicKey.Import(
                    Ed25519, publicKeyBytes, KeyBlobFormat.RawPublicKey);

                return Ed25519.Verify(pubKey, dataBytes, signatureBytes);
            }
            catch
            {
                return false;
            }
        }

        public KeySafe DeriveChild(uint index)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (IsStealthSubAccount)
                throw new InvalidOperationException("Stealth sub-accounts cannot derive child keys.");

            byte[] indexBytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(indexBytes, index);

            byte[] data = [(byte)0x01, .. _ed25519PrivateKey, .. indexBytes];
            byte[] hmac = HMACSHA512.HashData(_hdChainCode, data);

            byte[] childPrivateKey = hmac[..32];
            byte[] childChainCode = hmac[32..];

            return new KeySafe(childPrivateKey, childChainCode, isStealthSubAccount: true);
        }

        public (string ephemeralPublicKey, string stealthAddress) ComputeStealthPayment(
            string recipientStealthPublicKey)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (string.IsNullOrEmpty(recipientStealthPublicKey))
                throw new ArgumentException("Recipient stealth public key cannot be null or empty.", nameof(recipientStealthPublicKey));

            (Key ephemeralKey, string ephemeralPublicKeyStr) = GenerateX25519Key();

            using (ephemeralKey)
            {
                byte[] sharedSecret = ComputeX25519SharedSecret(ephemeralKey, Convert.FromBase64String(recipientStealthPublicKey));
                byte[] stealthAddressBytes = SHA256.HashData(sharedSecret);
                string stealthAddress = Convert.ToHexStringLower(stealthAddressBytes);
                return (ephemeralPublicKeyStr, stealthAddress);
            }
        }

        public bool IsStealthPaymentMine(string ephemeralPublicKey, string stealthAddress)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (IsStealthSubAccount)
                throw new InvalidOperationException("Stealth sub-accounts cannot scan for stealth payments.");
            if (_x25519Key is null)
                throw new InvalidOperationException("No X25519 key available for stealth scanning.");
            if (string.IsNullOrEmpty(ephemeralPublicKey))
                throw new ArgumentException("Ephemeral public key cannot be null or empty.", nameof(ephemeralPublicKey));
            if (string.IsNullOrEmpty(stealthAddress))
                throw new ArgumentException("Stealth address cannot be null or empty.", nameof(stealthAddress));

            try
            {
                byte[] sharedSecret = ComputeX25519SharedSecret(_x25519Key, Convert.FromBase64String(ephemeralPublicKey));
                byte[] expectedAddressBytes = SHA256.HashData(sharedSecret);
                string expectedAddress = Convert.ToHexStringLower(expectedAddressBytes);
                return string.Equals(expectedAddress, stealthAddress, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public KeySafe DeriveStealthSpendKeySafe(string ephemeralPublicKey)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (IsStealthSubAccount)
                throw new InvalidOperationException("Stealth sub-accounts cannot derive stealth spend keys.");
            if (_x25519Key is null)
                throw new InvalidOperationException("No X25519 key available.");
            if (string.IsNullOrEmpty(ephemeralPublicKey))
                throw new ArgumentException("Ephemeral public key cannot be null or empty.", nameof(ephemeralPublicKey));

            byte[] sharedSecret = ComputeX25519SharedSecret(_x25519Key, Convert.FromBase64String(ephemeralPublicKey));
            byte[] stealthSpendKey = HMACSHA256.HashData("HYDRON stealth spend"u8.ToArray(), sharedSecret);

            return new KeySafe(stealthSpendKey, hdChainCode: [], isStealthSubAccount: true);
        }

        public string RotateStealthKeyPair()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (IsStealthSubAccount)
                throw new InvalidOperationException("Stealth sub-accounts do not have a stealth key pair to rotate.");

            (Key newKey, string newPublicKey) = GenerateX25519Key();

            _x25519Key?.Dispose();
            _x25519Key = newKey;
            StealthPublicKey = newPublicKey;

            return newPublicKey;
        }

        public string ExportMasterSeed()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (IsStealthSubAccount || _hdMasterSeed is null)
                throw new InvalidOperationException("Stealth sub-accounts do not have an HD master seed.");

            return Convert.ToBase64String(_hdMasterSeed);
        }

        public string ExportStealthPrivateKey()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (IsStealthSubAccount || _x25519Key is null)
                throw new InvalidOperationException("Stealth sub-accounts do not have a stealth private key.");
            return Convert.ToBase64String(_x25519Key.Export(KeyBlobFormat.RawPrivateKey));
        }

        public string ExportPrivateKey()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return Convert.ToBase64String(_ed25519PrivateKey);
        }

        public static KeySafe ImportFromKeys(
            string privateKeyBase64,
            string stealthPrivateKeyBase64,
            string masterSeedBase64)
        {
            if (string.IsNullOrWhiteSpace(privateKeyBase64))
                throw new ArgumentException("Private key cannot be null or empty.", nameof(privateKeyBase64));
            if (string.IsNullOrWhiteSpace(stealthPrivateKeyBase64))
                throw new ArgumentException("Stealth private key cannot be null or empty.", nameof(stealthPrivateKeyBase64));
            if (string.IsNullOrWhiteSpace(masterSeedBase64))
                throw new ArgumentException("Master seed cannot be null or empty.", nameof(masterSeedBase64));

            try
            {
                return new KeySafe(
                    Convert.FromBase64String(privateKeyBase64),
                    Convert.FromBase64String(stealthPrivateKeyBase64),
                    Convert.FromBase64String(masterSeedBase64));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid key material format.", ex);
            }
        }

        public void Dispose()
        {
            if (IsDisposed) return;

            _x25519Key?.Dispose();
            CryptographicOperations.ZeroMemory(_ed25519PrivateKey);

            if (_hdMasterSeed is not null)
                CryptographicOperations.ZeroMemory(_hdMasterSeed);

            _disposed = 1;
        }

        private static byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }

        private static (byte[] privateKey, string publicKeyBase64) GenerateEd25519KeyPair()
        {
            using Key key = Key.Create(Ed25519, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
            byte[] privateKeyBytes = key.Export(KeyBlobFormat.RawPrivateKey);
            byte[] publicKeyBytes = key.PublicKey.Export(KeyBlobFormat.RawPublicKey);
            return (privateKeyBytes, Convert.ToBase64String(publicKeyBytes));
        }

        private static (Key key, string publicKeyBase64) GenerateX25519Key()
        {
            Key key = Key.Create(X25519, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
            string publicKeyBase64 = Convert.ToBase64String(key.PublicKey.Export(KeyBlobFormat.RawPublicKey));
            return (key, publicKeyBase64);
        }

        private static string DeriveEd25519PublicKey(byte[] privateKeyBytes)
        {
            using Key key = Key.Import(Ed25519, privateKeyBytes, KeyBlobFormat.RawPrivateKey,
                new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
            return Convert.ToBase64String(key.PublicKey.Export(KeyBlobFormat.RawPublicKey));
        }

        private static string DeriveAddress(string publicKeyBase64)
        {
            byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
            byte[] hash = SHA256.HashData(publicKeyBytes);
            return Convert.ToHexStringLower(hash);
        }

        private static byte[] DeriveHdMasterChainCode(byte[] masterSeed)
        {
            byte[] hmac = HMACSHA512.HashData(Encoding.UTF8.GetBytes(HdHmacKey), masterSeed);
            return hmac[32..];
        }

        private static byte[] ComputeX25519SharedSecret(Key privateKey, byte[] peerPublicKeyBytes)
        {
            NSec.Cryptography.PublicKey peerPublicKey = NSec.Cryptography.PublicKey.Import(
                X25519, peerPublicKeyBytes, KeyBlobFormat.RawPublicKey);

            SharedSecret shared = X25519.Agree(privateKey, peerPublicKey)
                ?? throw new InvalidOperationException("X25519 key agreement failed.");

            using (shared)
            {
                KeyDerivationAlgorithm hkdf = KeyDerivationAlgorithm.HkdfSha256;
                byte[] output = new byte[32];
                hkdf.DeriveBytes(shared, [], "HYDRON stealth"u8, output);
                return output;
            }
        }

        public override string ToString() =>
            $"KEYSAFE (Address: {Address} | Public Key: {PublicKey} | Stealth Public Key: {StealthPublicKey ?? "N/A"} | Is Stealth Sub-Account: {IsStealthSubAccount})";
    }
}
