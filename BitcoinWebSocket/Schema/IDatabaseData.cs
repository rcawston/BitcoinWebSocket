using LiteDB;

namespace BitcoinWebSocket.Schema
{
    /// <summary>
    ///     Indicates classes that can be inserted into the database
    /// </summary>
    public interface IDatabaseData
    {
        ObjectId Id { get; set; }
    }
}
