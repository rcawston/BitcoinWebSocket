using System.Configuration;
using BitcoinWebSocket.Consumer;

namespace BitcoinWebSocket
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // get ZMQ server address from app settings
            var ZMQServerTX = ConfigurationManager.AppSettings["ZMQPublisherRawTX"];
            var ZMQServerBlock = ConfigurationManager.AppSettings["ZMQPublisherRawBlock"];
            // start ZMQ subscribers
            var rawTXSubscriber = new ZMQ.Subscriber(ZMQServerTX, "rawtx", new TXConsumer());
            var rawBlockSubscriber = new ZMQ.Subscriber(ZMQServerBlock, "rawblock", new BlockConsumer());
        }
    }
}
