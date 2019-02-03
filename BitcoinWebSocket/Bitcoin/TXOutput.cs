using BitcoinWebSocket.Util;

namespace BitcoinWebSocket.Bitcoin
{
    public enum OutputType
    {
        P2PH,
        P2SH,
        P2WPKH,
        P2WSH,
        DATA,
        OTHER
    }

    public class TXOutput
    {
        public long Value { get; set; }
        public OutputType Type { get; private set; }
        public string Address { get; private set; }

        public byte[] Script
        {
            get => _script.ScriptBytes;
            set
            {
                _script = new Script(value);
                Type = GetOutputType();
                switch (Type)
                {
                    case OutputType.P2PH:
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

        private Script _script;

        private OutputType GetOutputType()
        {
            if (_script.OpCodes[0] == OpCodeType.OP_RETURN)
                return OutputType.DATA;

            switch (_script.OpCodes.Count)
            {
                // P2PH - Pay to PubkeyHash
                // OP_DUP OP_HASH160 <address> OP_EQUALVERIFY OP_CHECKSIG
                case 5 when
                    _script.OpCodes[0] == OpCodeType.OP_DUP &&
                    _script.OpCodes[1] == OpCodeType.OP_HASH160 &&
                    _script.OpCodes[2] == OpCodeType.OP_DATA &&
                    _script.OpCodes[3] == OpCodeType.OP_EQUALVERIFY &&
                    _script.OpCodes[4] == OpCodeType.OP_CHECKSIG:
                    return OutputType.P2PH;

                // P2SH - Pay to ScriptHash
                // OP_HASH160 <address=Hash160(RedeemScript)> OP_EQUAL
                case 3 when
                    _script.OpCodes[0] == OpCodeType.OP_HASH160 &&
                    _script.OpCodes[1] == OpCodeType.OP_DATA &&
                    _script.OpCodes[2] == OpCodeType.OP_EQUAL:
                    return OutputType.P2SH;

                // P2WPKH 
                // OP_0 <address=20bytes>
                case 2 when
                    _script.OpCodes[0] == OpCodeType.OP_0 &&
                    _script.OpCodes[1] == OpCodeType.OP_DATA &&
                    _script.DataChunks[0].Length == 20:
                    return OutputType.P2WPKH;

                // P2WSH 
                // OP_0 <address=32bytes>
                case 2 when
                    _script.OpCodes[0] == OpCodeType.OP_0 &&
                    _script.OpCodes[1] == OpCodeType.OP_DATA &&
                    _script.DataChunks[0].Length == 32:
                    return OutputType.P2WSH;

                default:
                    return OutputType.OTHER;
            }
        }
    }
}