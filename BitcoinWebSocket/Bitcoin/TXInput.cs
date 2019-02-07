namespace BitcoinWebSocket.Bitcoin
{
    /// <summary>
    ///     Represents a Bitcoin Transaction Input
    /// </summary>
    public class TXInput
    {
        // hash of the transaction that created the UTXO being used
        public byte[] Hash;
        // index of the output in the previous transaction
        public uint Index;
        // storage of the output script
        private Script _script;
        // public getter/setter for the output script
        public byte[] Script
        {
            get => _script.ScriptBytes;
            set => _script = new Script(value);
        }
        // sequence number - used with non-max transaction lock_time to indicate opt-in RBF
        // https://github.com/bitcoin/bips/blob/master/bip-0125.mediawiki
        public uint Sequence;
        // the witness data for a segwit spend
        public byte[] Witness = new byte[0];
    }
}