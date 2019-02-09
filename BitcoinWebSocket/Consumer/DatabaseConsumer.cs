using System;
using System.Linq;
using BitcoinWebSocket.Bitcoin;
using BitcoinWebSocket.Schema;
using LiteDB;

namespace BitcoinWebSocket.Consumer
{
    /// <summary>
    ///     Represents a pending database write
    /// </summary>
    public class DatabaseWrite
    {
        public IDatabaseData Data { get; private set; }

        public DatabaseWrite(IDatabaseData data)
        {
            Data = data;
        }
    }

    /// <summary>
    ///     A consumer thread that processes database functions
    /// </summary>
    public class DatabaseConsumer : Consumer<DatabaseWrite>
    {
        // litedb database
        private readonly LiteDatabase _database;
        private readonly LiteCollection<Transaction> _transactions;
        private readonly LiteCollection<Block> _blocks;
        private readonly LiteCollection<Subscription> _subscriptions;

        /// <summary>
        ///     Constructor
        ///     - initializes LiteDB database
        ///     - opens collects for transactions and blocks, and ensures indexes
        /// </summary>
        /// <param name="fileName"></param>
        public DatabaseConsumer(string fileName) : base()
        {
            _database = new LiteDatabase(fileName);
            _transactions = _database.GetCollection<Transaction>("transactions");
            _transactions.EnsureIndex(x => x.TXIDHex, true);
            _blocks = _database.GetCollection<Block>("block");
            _blocks.EnsureIndex(x => x.BlockHash, true);
            _subscriptions = _database.GetCollection<Subscription>("subscriptions");
            _subscriptions.EnsureIndex(x => x.subTo, true);

            var mapper = BsonMapper.Global;
            // exclude fields Transactions and LengthMatch from block database storage
            mapper.Entity<Block>().Ignore(x => x.Transactions).Ignore(x => x.LengthMatch);
            // exclude fields and LengthMatch from block database storage
            mapper.Entity<Transaction>().Ignore(x => x.LengthMatch);
        }

        /// <summary>
        ///     Gets all subscriptions from the database
        /// </summary>
        /// <returns></returns>
        public Subscription[] GetSubscriptions()
        {
            return _subscriptions.FindAll().ToArray();
        }
        
        /// <summary>
        ///     Gets all subscriptions from the database
        /// </summary>
        /// <returns></returns>
        public Block GetLastBlock()
        {
            var lastHeight = _blocks.Max(x => x.Height);
            return _blocks.FindOne(x => x.Height == lastHeight);
        }

        /// <summary>
        ///     Processes database writes
        ///     - 
        /// </summary>
        /// <param name="data"></param>
        public override void DoWork(DatabaseWrite data)
        {
            // is this transaction data to write out?
            if (data.Data.GetType() == typeof(Transaction))
            {
                var transaction = (Transaction) data.Data;

                // does an identical transaction already exist in the db?
                var txSearch = _transactions.Find(x => x.TXIDHex == transaction.TXIDHex);
                if (txSearch.Any())
                {
                    // yes; so, if we know the inclusion block now, update the inclusion block and last updated timestamp
                    if (transaction.IncludedInBlock != null)
                    {
                        var tx = txSearch.First();
                        tx.IncludedInBlock = transaction.IncludedInBlock;
                        tx.LastUpdated = transaction.LastUpdated;
                        tx.TXIDHex = transaction.TXIDHex;
                        _transactions.Update(tx);
                    }
                }
                else
                {
                    // no; so, add the transaction
                    _transactions.Insert(transaction);
                }
            }

            // is this block data?
            if (data.Data.GetType() == typeof(Block))
            {
                var block = (Block) data.Data;

                // have we seen this block before?
                var blockSearch = _blocks.FindOne(x => x.BlockHash == block.BlockHash);
                if (blockSearch != null)
                {
                    // yes; we've seen the block before. So, no need to handle it.
                    return;
                }

                // is this the first block we've stored?
                blockSearch = _blocks.FindOne(x => x.LengthMatch);
                if (blockSearch == null)
                {
                    // yes; so, just store it without PrevHash check
                    // use JSON RPC to fetch block height
                    var blockHeight = Program.RPCClient.GetBlockHeight(block.BlockHash);
                    block.Height = blockHeight;
                    // save to db
                    _blocks.Insert(block);
                }
                else
                {
                    // no; so, check if we have the previous block in the chain
                    blockSearch = _blocks.FindOne(x => x.BlockHash == block.Header.PrevBlockHash);
                    if (blockSearch == null)
                    {
                        // TODO: we are missing the prev block; this shouldn't happen...
                        var lastHeight = _blocks.Max(x => x.Height);
                        blockSearch = _blocks.FindOne(x => x.Height == lastHeight);
                        // TODO: do a re-scan from the last seen block
                    }
                    else
                    {
                        // block height is prevblock height + 1
                        block.Height = blockSearch.Height + 1;
                        // save to db
                        _blocks.Insert(block);
                    }
                }
            }

            // is this a subscription request?
            if (data.Data.GetType() == typeof(Subscription))
            {
                // yes, so add it to the database
                var subscription = (Subscription) data.Data;
                try
                {
                    _subscriptions.Insert(subscription);
                }
                catch (LiteException e)
                {
                    if (e.ErrorCode == 110)
                    {
                        // duplicate key; subscription already exists
                    }
                }
            }
        }
    }
}