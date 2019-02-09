using System;
using BitcoinWebSocket.Bitcoin;
using BitcoinWebSocket.Util;

namespace BitcoinWebSocket.Consumer
{
    /// <inheritdoc />
    /// <summary>
    ///     A consumer thread that processes raw transactions
    /// </summary>
    public class TXConsumer : Consumer<byte[]>
    {
        /// <summary>
        ///     Processes raw bitcoin transactions
        ///     - decodes the transaction, output scripts, and output addresses
        /// </summary>
        /// <param name="data">raw transaction byte array</param>
        public override void DoWork(byte[] data)
        {
            // hex version of the transaction
            var txHex = ByteToHex.ByteArrayToHex(data);

            // attempt to decode the transaction (also decodes output scripts and addresses)
            Transaction transaction;
            try
            {
                transaction = new Transaction(data)
                {
                    FirstSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to decode transaction: \n" + txHex);
                return;
            }

            // check all outputs of the transaction
            SubscriptionCheck.CheckForSubscription(transaction);

            /*
            Console.WriteLine("Received Transaction with " + transaction.Outputs.Length + " outputs and "
                              + transaction.Inputs.Length + " inputs. HasWitness = " +
                              (transaction.HasWitness ? "YES" : "NO") +
                              ". Output Scripts:");


            // iterate over each output in the transaction
            foreach (var output in transaction.Outputs)
            {
                // write out the raw output script as hex
                Console.WriteLine(ByteToHex.ByteArrayToHex(output.Script));
                var script = new Script(output.Script);
                var dataCount = 0;
                // write out the ASM version of the output
                foreach (var opCode in script.OpCodes)
                    if (opCode == OpCodeType.OP_DATA)
                        Console.Write(" " + ByteToHex.ByteArrayToHex(script.DataChunks[dataCount++]));
                    else
                        Console.Write(" " + opCode);
                Console.WriteLine();
                // write out the type and address (if the output is a known payment type)
                Console.WriteLine(" Type = " + output.Type + (output.Address == "" ? "" : ". Address = " + output.Address));
            }
            Console.WriteLine();
            */
        }
    }
}