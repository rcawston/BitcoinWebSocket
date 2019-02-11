using System.Collections.Generic;
using System.Linq;
using BitcoinWebSocket.Bitcoin;
using BitcoinWebSocket.Schema;
using BitcoinWebSocket.Util;
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
        // litedb collections
        private readonly LiteCollection<Transaction> _transactions;
        private readonly LiteCollection<Block> _blocks;
        private readonly LiteCollection<Subscription> _subscriptions;
        private string _chainTipHash;

        /// <summary>
        ///     Constructor
        ///     - initializes LiteDB database
        ///     - opens collects for transactions and blocks, and ensures indexes
        /// </summary>
        /// <param name="fileName"></param>
        public DatabaseConsumer(string fileName) : base()
        {
            var database = new LiteDatabase(fileName);
            _transactions = database.GetCollection<Transaction>("transactions");
            _transactions.EnsureIndex(x => x.TXIDHex, true);
            _blocks = database.GetCollection<Block>("block");
            _blocks.EnsureIndex(x => x.BlockHash, true);
            _blocks.EnsureIndex(x => x.Height);
            _subscriptions = database.GetCollection<Subscription>("subscriptions");
            _subscriptions.EnsureIndex(x => x.subTo, true);

            var mapper = BsonMapper.Global;
            // exclude fields Transactions and LengthMatch from block database storage
            mapper.Entity<Block>().Ignore(x => x.Transactions).Ignore(x => x.LengthMatch);
            // exclude field LengthMatch from block database storage
            mapper.Entity<Transaction>().Ignore(x => x.LengthMatch);
            // get the chaintip hash
            var chainTip = _blocks.FindOne(x => x.IsChainTip);
            if (chainTip != null)
                _chainTipHash = chainTip.BlockHash;
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
            return lastHeight.AsInt64 == 0 ? null : _blocks.FindOne(x => x.Height == lastHeight);
        }

        /// <summary>
        ///     Processes database writes
        ///     - 
        /// </summary>
        /// <param name="data">DatabaseWrite for Transaction, Block, or Subscription</param>
        public override void DoWork(DatabaseWrite data)
        {
            // is this transaction data to write out?
            if (data.Data.GetType() == typeof(Transaction))
            {
                var transaction = (Transaction) data.Data;
                HandleTransaction(transaction);
            }
            // is this block data?
            else if (data.Data.GetType() == typeof(Block))
            {
                var block = (Block) data.Data;
                HandleBlock(block);
            }
            // is this a subscription request?
            else if (data.Data.GetType() == typeof(Subscription))
            {
                var subscription = (Subscription) data.Data;
                HandleSubscription(subscription);
            }
        }

        private void HandleBlock(Block block)
        {
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
                // mark as best known chain tip
                _chainTipHash = block.BlockHash;
                block.IsChainTip = true;
                // save to db
                _blocks.Insert(block);
                return;
            }

            // no; so, check if we have the previous block in the chain
            blockSearch = _blocks.FindOne(x => x.BlockHash == block.Header.PrevBlockHash);
            if (blockSearch == null)
            {
                // we are missing the prev block; this shouldn't happen...
                // queue blocks backwards until we find a prev-block we have, and track the missing blocks
                var missingBlocks = new List<Block> {block};
                var prevBlockHash = block.Header.PrevBlockHash;
                // loop until we have already have the previous block
                while (!_blocks.Exists(x => x.BlockHash == prevBlockHash))
                {
                    var prevBlockData = Program.RPCClient.GetBlockData(block.Header.PrevBlockHash);
                    var prevBlock = new Block(ByteToHex.StringToByteArray(prevBlockData));
                    missingBlocks.Add(prevBlock);
                    prevBlockHash = prevBlock.Header.PrevBlockHash;
                }
                // missingBlocks is now an ordered list of blocks we are missing - queue them in reverse
                missingBlocks.Reverse();
                foreach (var missingBlock in missingBlocks)
                {
                    Program.Database.EnqueueTask(new DatabaseWrite(missingBlock), 0);
                }

                // discard (don't save) this block, as it will be re-processed in order
                return;
            }

            // we already have the previous block in the chain...
            // so, this new block's height is prevblock height + 1
            block.Height = blockSearch.Height + 1;
            var chainTipBlock = _blocks.FindOne(x => x.IsChainTip);
            // check if the prevHash block is our chaintip
            if (_chainTipHash != block.Header.PrevBlockHash)
            {
                // no; so, there was a re-org!
                // we need to invalidate transaction inclusions, back to the forking block
                var orphanedBlock = chainTipBlock;
                var newChainBlock = block;
                var orphanedBlocks = new List<Block> { orphanedBlock };
                var newChainBlocks = new List<Block> { newChainBlock };
                // step backwards on each chain in turn until the two sides of the fork are at the same height
                while (orphanedBlock.Height > newChainBlock.Height)
                {
                    orphanedBlock = _blocks.FindOne(x => x.BlockHash == orphanedBlock.Header.PrevBlockHash);
                    orphanedBlocks.Add(orphanedBlock);
                }
                while (orphanedBlock.Height < newChainBlock.Height)
                {
                    newChainBlock = _blocks.FindOne(x => x.BlockHash == newChainBlock.Header.PrevBlockHash);
                    newChainBlocks.Add(newChainBlock);
                }
                // orphaned chain and new chain are the same height now
                // step back both chains at the same time until we have a matching prevBlockHash
                while (orphanedBlock.Header.PrevBlockHash != newChainBlock.Header.PrevBlockHash)
                {
                    orphanedBlock = _blocks.FindOne(x => x.BlockHash == orphanedBlock.Header.PrevBlockHash);
                    orphanedBlocks.Add(orphanedBlock);
                    newChainBlock = _blocks.FindOne(x => x.BlockHash == newChainBlock.Header.PrevBlockHash);
                    newChainBlocks.Add(newChainBlock);
                }
                // prevBlockHash is now the forking block;
                // roll-back transaction inclusions
                var transactions = _transactions.Find(x => x.IncludedAtBlockHeight >= orphanedBlock.Height);
                foreach (var transaction in transactions)
                {
                    transaction.IncludedAtBlockHeight = 0;
                    transaction.IncludedInBlockHex = "";
                }

                // mark all blocks on the orphaned side as orphaned, and vice-versa
                foreach (var blk in orphanedBlocks)
                    blk.Orphaned = true;
                // this is needed in the case of re-re-orgs
                foreach (var blk in newChainBlocks)
                    blk.Orphaned = false;

                // we need to re-scan transactions in higher blocks
                // (skip the transactions in this block itself, as they will be queued behind this insert)
                // for most re-orgs, this won't actually have anything to process
                foreach (var blk in newChainBlocks.Where(x => x.BlockHash != block.BlockHash))
                {
                    // check all transactions in the block
                    foreach (var transaction in block.Transactions)
                        SubscriptionCheck.CheckForSubscription(transaction);
                }

                // re-org is handled, and this is the new chaintip, so fall thru to insert the block normally
            }

            // this is a regular block insert - we have previous block, and the previous block is our last known chaintip
            chainTipBlock.IsChainTip = false;
            _blocks.Update(blockSearch);
            block.IsChainTip = true;
            _chainTipHash = block.BlockHash;
            // save to db
            _blocks.Insert(block);
        }

        private void HandleTransaction(Transaction transaction)
        {
            // does an identical transaction already exist in the db?
            var txSearch = _transactions.Find(x => x.TXIDHex == transaction.TXIDHex);
            if (txSearch.Any())
            {
                // yes; so, if we know the inclusion block now, update the inclusion block and last updated timestamp
                if (transaction.IncludedInBlockHex == null) return;
                // get the height from db
                var blockSearch = _blocks.FindOne(x => x.BlockHash == transaction.IncludedInBlockHex);
                // update the db with new inclusion information
                var tx = txSearch.First();
                tx.IncludedInBlockHex = transaction.IncludedInBlockHex;
                tx.LastUpdated = transaction.LastUpdated;
                tx.TXIDHex = transaction.TXIDHex;
                tx.IncludedAtBlockHeight = blockSearch.Height;
                // save update to db
                _transactions.Update(tx);
            }
            else
            {
                // no; so, add the transaction
                _transactions.Insert(transaction);
            }
        }

        private void HandleSubscription(Subscription subscription)
        {
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
