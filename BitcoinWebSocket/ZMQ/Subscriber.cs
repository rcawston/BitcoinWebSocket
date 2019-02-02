using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;

namespace BitcoinWebSocket.ZMQ
{
    /// <inheritdoc />
    /// <summary>
    ///     ZMQ.Subscriber creates threaded workers that subscribes to the ZMQ publisher
    ///     - listens for specified frame
    ///     - passes received frames to threaded consumer
    /// </summary>
    public class Subscriber : IDisposable
    {
        private readonly Thread _worker;
        private readonly string _zmqConnectTo;
        private readonly string _zmqSubscribeTopic;
        private readonly Consumer.Consumer _consumer;

        /// <summary>
        ///     Constructor
        /// </summary>
        public Subscriber(string connectTo, string subscribeTo, Consumer.Consumer consumer)
        {
            _zmqConnectTo = connectTo;
            _zmqSubscribeTopic = subscribeTo;
            // start the consumer
            _consumer = consumer;
            // start the ZMQ SUB worker
            _worker = new Thread(SubscribeWorker);
            _worker.Start();
        }

        public void Dispose()
        {
            _consumer.Dispose();
            _worker.Abort();
        }

        /// <summary>
        ///     Subscription worker
        ///     - Creates ZMQ subscriber socket and listens for specified frames
        /// </summary>
        private void SubscribeWorker()
        {
            using (var subscriber = new SubscriberSocket())
            {
                subscriber.Connect(_zmqConnectTo);
                subscriber.Subscribe(_zmqSubscribeTopic);
                Console.WriteLine("Subscribed to ZeroMQ '" + _zmqConnectTo + "' publisher for '" + _zmqSubscribeTopic + "' frames.");
                     
                while (true)
                {
                    // we expect the publisher to sends 3 frames per data packet
                    // first, the header frame
                    var replyHeader = subscriber.ReceiveFrameString();
                    if (replyHeader != _zmqSubscribeTopic)
                    {
                        Console.WriteLine("Unexpected Frame Header! '"+ _zmqSubscribeTopic+"' expected, but received " + replyHeader +
                                          ". Skipping and waiting for proper frame header.");
                        continue;
                    }

                    // next, the data frame
                    var replyFrame = subscriber.ReceiveFrameBytes();

                    // last, the reply counter frame
                    var replyCounter = BitConverter.ToInt32(subscriber.ReceiveFrameBytes(), 0);
                    // Console.WriteLine("counter: " + replyCounter);
                    
                    // pass the received data to our consumer thread
                    _consumer.EnqueueTask(replyFrame, replyCounter);

                }
            }
        }
    }
}
