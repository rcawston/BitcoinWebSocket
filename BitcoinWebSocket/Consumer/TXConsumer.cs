﻿using System;
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
            foreach (var output in transaction.Outputs) // does this transaction contain an output we are watching?
                if (Program.WebSocketServer.Subscriptions.Exists(a =>
                    a.type == SubscriptionType.ADDRESS && a.subTo == output.Address))
                {
                    // yes; so, broadcast an update for this transaction
                    Program.WebSocketServer.BroadcastTransaction(transaction, output.Address);

                    // save the transaction in the database
                    Program.Database.EnqueueTask(new DatabaseWrite(transaction), 0);
                }
                // does this transaction include OP_RETURN data?
                else if (output.Type == OutputType.DATA)
                {
                    // are there subscriptions to the OP_RETURN data prefix?
                    var subs = Program.WebSocketServer.Subscriptions.FindAll(a =>
                        a.type == SubscriptionType.OP_RETURN_PREFIX &&
                        output.ScriptDataHex.StartsWith(a.subTo, StringComparison.InvariantCultureIgnoreCase));

                    foreach (var sub in subs)
                        // yes; so, broadcast an update for this OP_RETURN
                        Program.WebSocketServer.BroadcastOpReturn(transaction, sub.subTo);

                    // save the transaction in the database
                    Program.Database.EnqueueTask(new DatabaseWrite(transaction), 0);
                }

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