using System;
using BitcoinWebSocket.Bitcoin;
using BitcoinWebSocket.Util;

namespace BitcoinWebSocket.Consumer
{
    /// <inheritdoc />
    /// <summary>
    ///     A consumer thread that processes raw blocks
    /// </summary>
    public class BlockConsumer : Consumer<byte[]>
    {
        /// <summary>
        ///     Processes raw bitcoin blocks
        ///     - decodes the block, transactions, output scripts, and output addresses
        /// </summary>
        /// <param name="data">raw block byte array</param>
        public override void DoWork(byte[] data)
        {
            // attempt to decode the block (also decodes transactions, output scripts, and addresses)
            Block block;
            try
            {
                block = new Block(data)
                {
                    FirstSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to decode block!");
                return;
            }

            Console.WriteLine("Received Block with " + block.Transactions.Length + " transactions.\n" +
                              " Previous Block: " + block.Header.PrevBlockHash + "\n" +
                              " Block Hash: " + block.BlockHash + "\n" +
                              ". Length Validated = " + (block.LengthMatch ? "YES" : "NO"));

            foreach (var transaction in block.Transactions)
            {
                // check all outputs of the transaction
                SubscriptionCheck.CheckForSubscription(transaction);
            }

            // Add block data to internal db, and check for re-org
            Program.Database.EnqueueTask(new DatabaseWrite(block), 0);
        }
    }
}