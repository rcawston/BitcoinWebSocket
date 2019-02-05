using System;
using BitcoinWebSocket.Bitcoin;
using BitcoinWebSocket.Util;
using System.Security.Cryptography;

namespace BitcoinWebSocket.Consumer
{
    /// <inheritdoc />
    /// <summary>
    ///     A consumer thread that processes raw block
    /// </summary>
    public class BlockConsumer : Consumer
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
                block = new Block(data);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to decode block!");
                return;
            }

            Console.WriteLine(ByteToHex.ByteArrayToHex(data));

            Console.WriteLine("Received Block with " + block.Transactions.Length + " transactions.\n" +
                              " Previous Block: " + ByteToHex.ByteArrayToHex(block.Header.PrevBlockHash) + "\n" +
                              " Block Hash: " + ByteToHex.ByteArrayToHex(block.BlockHash) + "\n" +
                              ". Length Validated = " + (block.LengthMatch ? "YES" : "NO"));


            // TODO: add header data to internal db, and check for re-org
        }
    }
}