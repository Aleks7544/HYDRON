namespace HYDRON.Database.Dtos
{
    internal class AccountDto
    {
        public string Address { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string StealthPublicKey { get; set; } = string.Empty;
        public string? Handle { get; set; }
        public string Balance { get; set; } = "0";
        public string Nonce { get; set; } = "0";
    }
}