namespace HYDRON.Database.Dtos
{
    internal sealed class StateBlockDto
    {
        public string BlockNumber { get; set; } = "0";
        public string Hash { get; set; } = string.Empty;
        public string PreviousHash { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public string ProducerAddress { get; set; } = string.Empty;
        public string MerkleRoot { get; set; } = string.Empty;
        public string GlobalStateRoot { get; set; } = string.Empty;
        public string TotalFeesCollected { get; set; } = "0";
        public List<string> TransactionBlockHashes { get; set; } = [];
    }
}