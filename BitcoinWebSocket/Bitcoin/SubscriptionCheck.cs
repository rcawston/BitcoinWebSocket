using System;
using BitcoinWebSocket.Consumer;

namespace BitcoinWebSocket.Bitcoin
{
    public class SubscriptionCheck
    {
        public static void CheckForSubscription(Transaction transaction)
        {
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
        }
    }
}