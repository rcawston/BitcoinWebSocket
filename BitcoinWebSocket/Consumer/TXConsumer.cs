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
            Transaction transaction;
            try
            {
                transaction = new Transaction(data);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to decode transaction: \n" + txHex);
                return;
            }

            Console.WriteLine("Received Transaction with " + transaction.Outputs.Length + " outputs and "
                              + transaction.Inputs.Length + " inputs. HasWitness = " +
                              (transaction.HasWitness ? "YES" : "NO")
                              + ". Length Validated = " + (transaction.LengthMatch ? "YES" : "NO") +
                              ". Output Scripts:");

            // does this transaction contain an output we are watching?
            foreach (var output in transaction.Outputs)
            {
                Console.WriteLine(ByteToHex.ByteArrayToHex(output.Script));
                var script = new Script(output.Script);
                var dataCount = 0;
                foreach (var opCode in script.OpCodes)
                    if (opCode == OpCodeType.OP_DATA)
                        Console.Write(" " + ByteToHex.ByteArrayToHex(script.DataChunks[dataCount++]));
                    else
                        Console.Write(" " + opCode);
                Console.WriteLine();

                Console.WriteLine(" Type = " + output.Type + (output.Address == "" ? "" : ". Address = " + output.Address));
            }
            Console.WriteLine();
        }
    }
}