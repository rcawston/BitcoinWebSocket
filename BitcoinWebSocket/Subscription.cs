using BitcoinWebSocket.Schema;
using LiteDB;

namespace BitcoinWebSocket
{
    /// <summary>
    ///     Type of subscription request
    ///     - OP_RETURN_PREFIX - watches for an OP_RETURN where the data section starts with a given value
    ///     - ADDRESS - watches for outputs to a given address
    /// </summary>
    public enum SubscriptionType
    {
        OP_RETURN_PREFIX,
        ADDRESS
    }

    /// <summary>
    ///     Represents a subscription to an address or op_return prefix
    /// </summary>
    public class Subscription : IDatabaseData
    {
        //  address or prefix subscription
        public SubscriptionType type { get; set; }
        // address or prefix
        public string subTo { get; set; }
        // database object Id
        public ObjectId Id { get; set; }

        /// <inheritdoc />
        /// <summary>
        ///     Constructor
        ///     - creates an empty subscription object
        ///     - used for LiteDB queries
        /// </summary>
        public Subscription()
        {
        }

        /// <inheritdoc />
        /// <summary>
        ///     Constructor
        ///     - creates a subscription request
        /// </summary>
        public Subscription(string subTo, SubscriptionType type)
        {
            this.subTo = subTo;
            this.type = type;
        }
    }
}
