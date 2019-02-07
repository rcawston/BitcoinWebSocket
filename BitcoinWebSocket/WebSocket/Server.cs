using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinWebSocket.Bitcoin;
using BitcoinWebSocket.Consumer;
using BitcoinWebSocket.Schema;
using Fleck;
using Newtonsoft.Json;

namespace BitcoinWebSocket.WebSocket
{
    /// <summary>
    ///     Implements a WebSocket Server
    ///     - uses https://github.com/statianzo/Fleck library
    /// </summary>
    internal class Server
    {
        // all sockets that are open
        private readonly List<IWebSocketConnection> _allSockets = new List<IWebSocketConnection>();

        // open sockets and the subscriptions they have requested
        private readonly Dictionary<IWebSocketConnection, List<Subscription>> _socketSubscriptions =
            new Dictionary<IWebSocketConnection, List<Subscription>>();

        // a list of all subscriptions (a socket closing does not remove the subscription)
        public readonly List<Subscription> Subscriptions;

        /// <summary>
        ///     Constructor
        ///     - starts websocket server
        ///     - listens for socket opened, closed, and messages
        /// </summary>
        /// <param name="listenOn">WebSocket connection url/port e.g. "ws://localhost:8181"</param>
        /// <param name="subscriptions">Initial set of subscriptions</param>
        public Server(string listenOn, IEnumerable<Subscription> subscriptions)
        {
            Subscriptions = subscriptions.ToList();

            var server = new WebSocketServer(listenOn);
            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine("WebSocket Opened! Client: " + socket.ConnectionInfo.ClientIpAddress);
                    _allSockets.Add(socket);
                    _socketSubscriptions.Add(socket, new List<Subscription>());
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine("WebSocket Closed! Client: " + socket.ConnectionInfo.ClientIpAddress);
                    _allSockets.Remove(socket);
                    _socketSubscriptions.Remove(socket);
                };
                socket.OnMessage = message =>
                {
                    Console.WriteLine("WebSocket Message Received! " + message);
                    HandleMessage(message, socket);
                };
            });
        }

        /// <summary>
        ///     Broadcasts a transaction to subscribed websocket clients
        /// </summary>
        /// <param name="transaction">transaction to broadcast</param>
        /// <param name="socket">address that triggered a subscription match</param>
        public void BroadcastUpdate(Transaction transaction, IWebSocketConnection socket)
        {
            // create output objects for JSON conversion
            var outputs = new IOutgoingMessageOutput[transaction.Outputs.Length];
            for (var i = 0; i < transaction.Outputs.Length; i++)
            {
                if (transaction.Outputs[i].Type == OutputType.DATA)
                {
                    outputs[i] = new OutgoingMessageDataOutput
                    {
                        data = transaction.Outputs[i].ScriptDataHex,
                        type = transaction.Outputs[i].Type,
                        value = transaction.Outputs[i].Value
                    };
                }
                else
                {
                    outputs[i] = new OutgoingMessageTXOutput
                    {
                        addr = transaction.Outputs[i].Address,
                        type = transaction.Outputs[i].Type,
                        value = transaction.Outputs[i].Value
                    };
                }
            }

            // create JSON message and send to the subscribing websocket client
            socket.Send(JsonConvert.SerializeObject(new OutgoingTXMessage
            {
                op = transaction.IncludedInBlock == null ? "utx" : "ctx", // utx for unconfirmed, ctx for confirmed
                lock_time = transaction.LockTime,
                outputs = outputs,
                txid = transaction.TXIDHex,
                version = transaction.TXVersion,
                first_seen = transaction.FirstSeen,
                last_updated = transaction.LastUpdated
            }));
        }

        /// <summary>
        ///     Broadcasts a transaction to subscribed websocket clients
        /// </summary>
        /// <param name="transaction">transaction to broadcast</param>
        /// <param name="triggeringAddress">address that triggered a subscription match</param>
        public void BroadcastTransaction(Transaction transaction, string triggeringAddress)
        {
            // broadcast to each socket that has subscribed to the triggering address
            foreach (var socketSubscription in _socketSubscriptions.Where(x =>
                x.Value.Exists(y => y.type == SubscriptionType.ADDRESS && y.subTo == triggeringAddress)))
                BroadcastUpdate(transaction, socketSubscription.Key);
        }

        /// <summary>
        ///     Broadcasts a transaction to subscribed websocket clients
        /// </summary>
        /// <param name="transaction">transaction to broadcast</param>
        /// <param name="triggeringPrefix">op_return prefix that triggered a subscription match</param>
        public void BroadcastOpReturn(Transaction transaction, string triggeringPrefix)
        {
            // broadcast to each socket that has subscribed to the triggering address
            foreach (var socketSubscription in _socketSubscriptions.Where(x =>
                x.Value.Exists(y =>
                    y.type == SubscriptionType.OP_RETURN_PREFIX && y.subTo.StartsWith(triggeringPrefix))))
                BroadcastUpdate(transaction, socketSubscription.Key);
        }

        /// <summary>
        ///     Handles client websocket messages
        ///     - decodes JSON request
        /// </summary>
        /// <param name="message">message received from the client</param>
        /// <param name="socket">client socket that sent the message</param>
        private void HandleMessage(string message, IWebSocketConnection socket)
        {
            var request = JsonConvert.DeserializeObject<IncomingMessage>(message,
                new JsonSerializerSettings
                {
                    Error = delegate
                    {
                        socket.Send(JsonConvert.SerializeObject(new OutgoingError
                            {error = "Error decoding JSON WebSocket Request", op = "error"}));
                    }
                });

            switch (request.op)
            {
                // subscription request?
                case "addr_sub":
                case "data_sub":
                    if (_socketSubscriptions.TryGetValue(socket, out var val))
                    {
                        // record the subscription
                        var type = request.op == "data_sub"
                            ? SubscriptionType.OP_RETURN_PREFIX
                            : SubscriptionType.ADDRESS;
                        var subRequest = new Subscription(request.addr, type);
                        _socketSubscriptions[socket].Add(subRequest);
                        if (!Subscriptions.Exists(x => x.subTo == request.addr && x.type == type))
                            Subscriptions.Add(subRequest);
                        // save subscription to database
                        Program.Database.EnqueueTask(new DatabaseWrite(subRequest), 0);
                    }
                    else
                    {
                        socket.Send(JsonConvert.SerializeObject(new OutgoingError
                            {error = "Error with internal WebSocket state", op = "error"}));
                    }

                    break;

                // unsubscribe request?
                case "addr_unsub":
                case "data_unsub":
                    if (_socketSubscriptions.TryGetValue(socket, out _))
                        _socketSubscriptions[socket].RemoveAll(x =>
                            x.type == (request.op == "data_unsub"
                                ? SubscriptionType.OP_RETURN_PREFIX
                                : SubscriptionType.ADDRESS) && x.subTo.Equals(request.addr));
                    else
                        socket.Send(JsonConvert.SerializeObject(new OutgoingError
                            {error = "Error with internal WebSocket state", op = "error"}));
                    break;
                
                // simple ping/liveness check
                case "ping":
                    socket.Send("{\"op\": \"pong\"}");
                    break;
                
                // error for any other operation
                default:
                    socket.Send(JsonConvert.SerializeObject(new OutgoingError
                        { error = "Unknown operation", op = "error" }));
                    break;
            }
        }
    }
}