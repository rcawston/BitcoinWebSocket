using System;
using System.Configuration;
using System.Diagnostics;
using BitcoinWebSocket.Bitcoin;
using BitcoinWebSocket.Consumer;
using BitcoinWebSocket.Util;

namespace BitcoinWebSocket
{
    internal class Program
    {
        public static DatabaseConsumer Database;
        public static WebSocket.Server WebSocketServer;
        public static RPCClient RPCClient;

        private static void Main(string[] args)
        {
            // get internal litedb database filename from app settings
            var databaseFileName = ConfigurationManager.AppSettings["LiteDBFileName"];
            // init the database
            Database = new DatabaseConsumer(databaseFileName);
            // get saved subscriptions
            var subscriptions = Database.GetSubscriptions();

            // get RPC connection params from app settings
            var rpcURL = ConfigurationManager.AppSettings["RPCURI"];
            var rpcUsername = ConfigurationManager.AppSettings["RPCUsername"];
            var rpcPassword = ConfigurationManager.AppSettings["RPCPassword"];

            // init RPC connection
            RPCClient = new RPCClient(new Uri(rpcURL), rpcUsername, rpcPassword);
            // get info for the last block that was processed
            var lastBlock = Database.GetLastBlock();
            var blockCount = RPCClient.GetBlockCount();
            // write out of the current block height, and last processed block height
            Console.WriteLine("Current block: " + blockCount + ". " + (lastBlock == null ? " No previous block data found." : " Last block processed: " + lastBlock.Height));
            // if we already have block data, fetch transactions since that block
            if (lastBlock != null)
            {
                // record how long it takes to process the block data
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                // look at all blocks from the last block that was processed until the current height
                for (var blockIndex = lastBlock.Height; blockIndex < blockCount; blockIndex++)
                {
                    // fetch raw block data by height
                    var blockHash = RPCClient.GetBlockHash(blockIndex);
                    var blockData = RPCClient.GetBlockData(blockHash);
                    // decode the block
                    var block = new Block(ByteToHex.StringToByteArray(blockData))
                    {
                        FirstSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };
                    // process all transactions that occured in the block
                    foreach (var transaction in block.Transactions)
                    {
                        // does this transaction contain an output we are watching?
                        SubscriptionCheck.CheckForSubscription(transaction);
                    }
                    // add the block to the database
                    Database.EnqueueTask(new DatabaseWrite(block), 0);
                }
                
                stopWatch.Stop();
                var elapsed = stopWatch.Elapsed;
                Console.WriteLine("Processed blocks in " + $"{elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds / 10:00}");
            }

            // get websocket listen address/port from app settings
            var websocketListen = ConfigurationManager.AppSettings["WebSocketListen"];
            // start websocket server
            WebSocketServer = new WebSocket.Server(websocketListen, subscriptions);

            // get ZMQ server address from app settings
            var zmqServerTX = ConfigurationManager.AppSettings["ZMQPublisherRawTX"];
            var zmqServerBlock = ConfigurationManager.AppSettings["ZMQPublisherRawBlock"];

            // start ZMQ subscribers
            new ZMQ.Subscriber(zmqServerTX, "rawtx", new TXConsumer());
            new ZMQ.Subscriber(zmqServerBlock, "rawblock", new BlockConsumer());

            // skip scanning the mempool if there is no saved block data yet (TODO: maybe it should still scan?) 
            if (lastBlock != null)
            {
                // fetch the mempool
                var memPool = RPCClient.GetMemPool();
                Console.WriteLine("Mempool contains " + memPool.Length + " transactions; processing...");

                // record how long it takes to process the mempool
                var stopWatch2 = new Stopwatch();
                stopWatch2.Start();
                // process all mempool transactions
                foreach (var txid in memPool)
                {
                    var rawTransaction = RPCClient.GetRawTransaction(txid);
                    var transaction = new Transaction(rawTransaction)
                    {
                        FirstSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    // does this transaction contain an output we are watching?
                    SubscriptionCheck.CheckForSubscription(transaction);
                }

                stopWatch2.Stop();
                var elapsed2 = stopWatch2.Elapsed;
                Console.WriteLine("Processed mempool in " +
                                  $"{elapsed2.Hours:00}:{elapsed2.Minutes:00}:{elapsed2.Seconds:00}.{elapsed2.Milliseconds / 10:00}");
            }
            else
            {
                Console.WriteLine("Skipping mempool scan on first run.");
            }
        }
    }
}
