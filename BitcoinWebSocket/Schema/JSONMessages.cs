using BitcoinWebSocket.Bitcoin;

namespace BitcoinWebSocket.Schema
{
    /// <summary>
    ///     Represents an incoming JSON message
    /// </summary>
    public class IncomingMessage
    {
        public string op { get; set; }
        public string addr { get; set; }
    }

    public interface IOutgoingMessageOutput
    {
        // value spent to the output
        ulong value { get; set; }
        // type of output, if known type
        OutputType type { get; set; }
    }

    /// <summary>
    ///     Represents a transaction output in an outgoing JSON message
    /// </summary>
    public class OutgoingMessageTXOutput : IOutgoingMessageOutput
    {
        // value spent to the output
        public ulong value { get; set; }
        // type of output, if known type
        public OutputType type { get; set; }
        // base58 or bech32 formatted address to which the output spends
        public string addr { get; set; }
    }

    /// <summary>
    ///     Represents an OP_RETURN transaction output in an outgoing JSON message
    /// </summary>
    public class OutgoingMessageDataOutput : IOutgoingMessageOutput
    {
        // value spent to the output
        public ulong value { get; set; }
        // type of output, if known type
        public OutputType type { get; set; }
        // OP_RETURN data
        public string data { get; set; }
    }
    /// <summary>
    ///     Represents a transaction in an outgoing JSON message
    /// </summary>
    public class OutgoingTXMessage
    {
        public string op { get; set; }
        public long lock_time { get; set; }
        public uint version { get; set; }
        public string txid { get; set; }
        public IOutgoingMessageOutput[] outputs { get; set; }
    }

    /// <summary>
    ///     Represents a error in an outgoing JSON message
    /// </summary>
    public class OutgoingError
    {
        public string op { get; set; }
        public string error { get; set; }
    }
}