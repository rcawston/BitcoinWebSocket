namespace BitcoinWebSocket.Bitcoin
{
    /// <summary>
    ///     Represents a Bitcoin Transaction Input
    /// </summary>
    public class TXInput
    {
        public byte[] Hash;
        public int Index;
        public byte[] Script;
        public int Sequence;
        public byte[] Witness = new byte[0];
    }
}