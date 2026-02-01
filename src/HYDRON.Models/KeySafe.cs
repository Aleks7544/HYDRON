using NSec.Cryptography;
using System.Text;

namespace Hydron.Models
{
    public class KeySafe
    {
        public string Address { get; private set; } = null!;
        public string PublicKey { get; private set; } = null!;
        private string PrivateKey { get; set; } = null!;

        private static readonly SignatureAlgorithm Algorithm = SignatureAlgorithm.Ed25519;

        public KeySafe()
        {
            GenerateKeyPair();
        }

        private KeySafe(string privateKey)
        {
            RecoverFromPrivateKey(privateKey);
        }

        private void GenerateKeyPair()
        {
            using Key key = Key.Create(Algorithm, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
            byte[] privateKeyBytes = key.Export(KeyBlobFormat.RawPrivateKey);
            byte[] publicKeyBytes = key.PublicKey.Export(KeyBlobFormat.RawPublicKey);

            PrivateKey = Convert.ToBase64String(privateKeyBytes);
            PublicKey = Convert.ToBase64String(publicKeyBytes);
            Address = DeriveAddress(PublicKey);
        }

        private static string DeriveAddress(string publicKey)
        {
            byte[] publicKeyBytes = Convert.FromBase64String(publicKey);

            HashAlgorithm hashAlgorithm = HashAlgorithm.Sha256;
            byte[] hash = hashAlgorithm.Hash(publicKeyBytes);

            byte[] addressBytes = new byte[20];
            Array.Copy(hash, 0, addressBytes, 0, 20);

            return Convert.ToHexStringLower(addressBytes);
        }

        public string ExportPrivateKey() => PrivateKey;

        public static KeySafe ImportFromPrivateKey(string privateKeyText) => string.IsNullOrWhiteSpace(privateKeyText)
            ? throw new ArgumentException("Private key cannot be null or empty.", nameof(privateKeyText))
            : new KeySafe(privateKeyText);

        private void RecoverFromPrivateKey(string privateKey)
        {
            try
            {
                byte[] privateKeyBytes = Convert.FromBase64String(privateKey);

                using Key key = Key.Import(Algorithm, privateKeyBytes, KeyBlobFormat.RawPrivateKey, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
                byte[] publicKeyBytes = key.PublicKey.Export(KeyBlobFormat.RawPublicKey);

                PrivateKey = privateKey;
                PublicKey = Convert.ToBase64String(publicKeyBytes);
                Address = DeriveAddress(PublicKey);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid private key format.", ex);
            }
        }

        public string SignTransaction(Transaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);

            string txData = $"{transaction.Sender}{transaction.Receiver}{transaction.Amount}{transaction.Nonce}{transaction.Fee}{transaction.Timestamp:O}";
            byte[] dataBytes = Encoding.UTF8.GetBytes(txData);

            byte[] privateKeyBytes = Convert.FromBase64String(PrivateKey);

            using Key key = Key.Import(Algorithm, privateKeyBytes, KeyBlobFormat.RawPrivateKey);
            byte[] signature = Algorithm.Sign(key, dataBytes);
            return Convert.ToBase64String(signature);
        }

        public static bool VerifySignature(Transaction transaction, string signature, string publicKey)
        {
            ArgumentNullException.ThrowIfNull(transaction);

            if (string.IsNullOrWhiteSpace(signature))
            {
                throw new ArgumentException("Signature cannot be null or empty.", nameof(signature));
            }

            if (string.IsNullOrWhiteSpace(publicKey))
            {
                throw new ArgumentException("Public key cannot be null or empty.", nameof(publicKey));
            }

            try
            {
                string txData = $"{transaction.Sender}{transaction.Receiver}{transaction.Amount}{transaction.Nonce}{transaction.Fee}{transaction.Timestamp:O}";
                byte[] dataBytes = Encoding.UTF8.GetBytes(txData);

                byte[] publicKeyBytes = Convert.FromBase64String(publicKey);
                byte[] signatureBytes = Convert.FromBase64String(signature);

                PublicKey pubKey = NSec.Cryptography.PublicKey.Import(Algorithm, publicKeyBytes, KeyBlobFormat.RawPublicKey);
                return Algorithm.Verify(pubKey, dataBytes, signatureBytes);
            }
            catch
            {
                return false;
            }
        }

        public override string ToString() => Address;
    }
}
