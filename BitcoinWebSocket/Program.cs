using System.Configuration;
using BitcoinWebSocket.Consumer;

namespace BitcoinWebSocket
{
    internal class Program
    {
        public static DatabaseConsumer Database;
        public static WebSocket.Server WebSocketServer;

        private static void Main(string[] args)
        {
            // get internal litedb database filename from app settings
            var databaseFileName = ConfigurationManager.AppSettings["LiteDBFileName"];
            // init the database
            Database = new DatabaseConsumer(databaseFileName);
            // get saved subscriptions
            var subscriptions = Database.GetSubscriptions();
            
            // get websocket listen address/port from app settings
            var websocketListen = ConfigurationManager.AppSettings["WebSocketListen"];
            // start websocket server
            WebSocketServer = new WebSocket.Server(websocketListen, subscriptions);

            // get ZMQ server address from app settings
            var zmqServerTX = ConfigurationManager.AppSettings["ZMQPublisherRawTX"];
            var zmqServerBlock = ConfigurationManager.AppSettings["ZMQPublisherRawBlock"];

            // start ZMQ subscribers
            new ZMQ.Subscriber(zmqServerTX, "rawtx", new TXConsumer());
            new ZMQ.Subscriber(zmqServerBlock, "rawblock", new BlockConsumer());
        }
    }
}
