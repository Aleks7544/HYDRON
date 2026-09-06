using NSec.Cryptography;
using System.Text;

namespace HYDRON.Cryptography
{
    public static class SignatureVerifier
    {
        private static readonly SignatureAlgorithm Ed25519 = SignatureAlgorithm.Ed25519;

        public static bool Verify(string canonicalData, string signatureBase64, string publicKeyBase64)
        {
            if (string.IsNullOrEmpty(canonicalData)) return false;
            if (string.IsNullOrEmpty(signatureBase64)) return false;
            if (string.IsNullOrEmpty(publicKeyBase64)) return false;

            try
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(canonicalData);
                byte[] signatureBytes = Convert.FromBase64String(signatureBase64);
                byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);

                if (signatureBytes.Length != CryptoConstants.Ed25519SignatureBytes) return false;
                if (publicKeyBytes.Length != CryptoConstants.Ed25519PublicKeyBytes) return false;

                PublicKey pubKey = PublicKey.Import(Ed25519, publicKeyBytes, KeyBlobFormat.RawPublicKey);
                return Ed25519.Verify(pubKey, dataBytes, signatureBytes);
            }
            catch
            {
                return false;
            }
        }

        public static bool VerifyBytes(byte[]? data, string signatureBase64, string publicKeyBase64)
        {
            if (data is null || data.Length == 0) return false;
            if (string.IsNullOrEmpty(signatureBase64)) return false;
            if (string.IsNullOrEmpty(publicKeyBase64)) return false;

            try
            {
                byte[] signatureBytes = Convert.FromBase64String(signatureBase64);
                byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);

                if (signatureBytes.Length != CryptoConstants.Ed25519SignatureBytes) return false;
                if (publicKeyBytes.Length != CryptoConstants.Ed25519PublicKeyBytes) return false;

                PublicKey pubKey = PublicKey.Import(Ed25519, publicKeyBytes, KeyBlobFormat.RawPublicKey);
                return Ed25519.Verify(pubKey, data, signatureBytes);
            }
            catch
            {
                return false;
            }
        }
    }
}