namespace BitcoinWebSocket.Bitcoin
{
    /// <summary>
    ///     Represents a Bitcoin Transaction Input
    /// </summary>
    public class TXInput
    {
        public byte[] Hash;
        public uint Index;
        public byte[] Script;
        public uint Sequence;
        public byte[] Witness = new byte[0];
    }
}