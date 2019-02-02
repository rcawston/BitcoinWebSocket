using System;
using System.Configuration;
using BitcoinWebSocket.Consumer;

namespace BitcoinWebSocket
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var ZMQServer = ConfigurationManager.AppSettings["ZMQPublisher"];
            var sub = new ZMQ.Subscriber(ZMQServer, "rawtx", new TXConsumer());
        }
    }
}
