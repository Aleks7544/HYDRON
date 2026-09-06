namespace HYDRON.Cryptography
{
    public static class CryptoConstants
    {
        public const int Ed25519PublicKeyBytes = 32;

        public const int Ed25519PrivateKeyBytes = 32;

        public const int Ed25519SignatureBytes = 64;

        public const int Sha256Bytes = 32;
        
        public const int Sha256HexLength = Sha256Bytes * 2;

        public const string GenesisPreviousHash =
            "0000000000000000000000000000000000000000000000000000000000000000";
    }
}