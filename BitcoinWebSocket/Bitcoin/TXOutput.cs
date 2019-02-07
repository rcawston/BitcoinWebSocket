using BitcoinWebSocket.Util;

namespace BitcoinWebSocket.Bitcoin
{
    /// <summary>
    ///     Known output types
    ///     - P2PKH - Pay-to-PublicKey-Hash
    ///     - P2SH - Pay-to-Script-Hash
    ///     - P2WPKH - Pay-to-Witness-PublicKey-Hash
    ///     - P2WSH - Pay-to-Witness-Script-Hash
    ///     - Data - OP_RETURN data output (non-transactional)
    ///     - Other - Unidentified output script
    /// </summary>
    public enum OutputType
    {
        P2PKH,
        P2SH,
        P2WPKH,
        P2WSH,
        DATA,
        OTHER
    }
    /// <summary>
    ///     Represents a Bitcoin Transaction Output
    /// </summary>
    public class TXOutput
    {
        // value spent to the output
        public ulong Value { get; set; }
        // type of output, if known type
        public OutputType Type { get; private set; }
        // base58 or bech32 formatted address to which the output spends
        public string Address { get; private set; }

        // storage of the output script
        private Script _script;

        public string ScriptDataHex => _script.DataChunks.Count > 0 ? ByteToHex.ByteArrayToHex(_script.DataChunks[0]) : null;

        // public getter/setter for the output script
        public byte[] Script
        {
            get => _script.ScriptBytes;
            set
            {
                // save and decode the output script
                _script = new Script(value);
                // identify the type of output script
                Type = GetOutputType();

                // decode the address data based on output type
                // P2PKH and P2SH use base58, and P2WPKH and P2WSH use bech32 format addresses
                // for anything else, set address to an empty string
                switch (Type)
                {
                    case OutputType.P2PKH:
                        Address = Base58.EncodeWithCheckSum(ArrayTools.ConcatArrays(new byte[1] { 0 }, _script.DataChunks[0]));
                        break;
                    case OutputType.P2SH:
                        Address = Base58.EncodeWithCheckSum(ArrayTools.ConcatArrays(new byte[1] { 5 } , _script.DataChunks[0]));
                        break;
                    case OutputType.P2WPKH:
                    case OutputType.P2WSH:
                        Address = Bech32.EncodeWithHeaderAndChecksum(0, _script.DataChunks[0]);
                        break;
                    case OutputType.DATA:
                    case OutputType.OTHER:
                    default:
                        Address = "";
                        break;
                }
            }
        }

        /// <summary>
        ///     Identifies the type of output for a bitcoin script
        /// </summary>
        /// <returns>type of output</returns>
        private OutputType GetOutputType()
        {
            // OP_RETURN Data output (non-transactional)
            if (_script.OpCodes[0] == OpCodeType.OP_RETURN)
                return OutputType.DATA;

            switch (_script.OpCodes.Count)
            {
                // P2PKH - Pay-to-PublicKey-Hash
                // OP_DUP OP_HASH160 <address> OP_EQUALVERIFY OP_CHECKSIG
                case 5 when
                    _script.OpCodes[0] == OpCodeType.OP_DUP &&
                    _script.OpCodes[1] == OpCodeType.OP_HASH160 &&
                    _script.OpCodes[2] == OpCodeType.OP_DATA &&
                    _script.OpCodes[3] == OpCodeType.OP_EQUALVERIFY &&
                    _script.OpCodes[4] == OpCodeType.OP_CHECKSIG:
                    return OutputType.P2PKH;

                // P2SH - Pay-to-Script-Hash
                // OP_HASH160 <address=Hash160(RedeemScript)> OP_EQUAL
                case 3 when
                    _script.OpCodes[0] == OpCodeType.OP_HASH160 &&
                    _script.OpCodes[1] == OpCodeType.OP_DATA &&
                    _script.OpCodes[2] == OpCodeType.OP_EQUAL:
                    return OutputType.P2SH;

                // P2WPKH - Pay-to-Witness-PublicKey-Hash
                // OP_0 <address=20bytes>
                case 2 when
                    _script.OpCodes[0] == OpCodeType.OP_0 &&
                    _script.OpCodes[1] == OpCodeType.OP_DATA &&
                    _script.DataChunks[0].Length == 20:
                    return OutputType.P2WPKH;

                // P2WSH - Pay-to-Witness-Script-Hash
                // OP_0 <address=32bytes>
                case 2 when
                    _script.OpCodes[0] == OpCodeType.OP_0 &&
                    _script.OpCodes[1] == OpCodeType.OP_DATA &&
                    _script.DataChunks[0].Length == 32:
                    return OutputType.P2WSH;

                // anything else
                default:
                    return OutputType.OTHER;
            }
        }
    }
}