using System;
using BitcoinWebSocket.Bitcoin;
using BitcoinWebSocket.Util;

namespace BitcoinWebSocket.Consumer
{
    public class TXConsumer : Consumer
    {
        public override void DoWork(byte[] data)
        {
            var txHex = ByteToHex.ByteArrayToHex(data);
            try
            {
                var transaction = new Transaction(data);

                Console.WriteLine("Received Transaction with " + transaction.Outputs.Length + " outputs and "
                                  + transaction.Inputs.Length + " inputs. HasWitness = " + (transaction.HasWitness ? "YES" : "NO")
                                  + ". Length Validated = " + (transaction.LengthMatch ? "YES" : "NO"));

                // does this transaction contain an output we are watching?
                foreach (var output in transaction.Outputs)
                {
                    // TODO: still need to decode the output scripts...
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to decode transaction: \n" + txHex);
            }
        }
    }
}