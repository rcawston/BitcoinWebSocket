using System.Collections.Generic;
using System.Threading;

namespace BitcoinWebSocket.Consumer
{
    /// <summary>
    ///     A consumer thread that waits for queued tasks and does processing when tasks are queued
    /// </summary>
    public abstract class Consumer
    {
        private readonly object _locker = new object();
        private readonly Queue<byte[]> _tasks = new Queue<byte[]>();
        private readonly EventWaitHandle _wh = new AutoResetEvent(false);
        private readonly Thread _worker;

        /// <summary>
        ///     Constructor
        ///     - Starts threaded consumer worker
        /// </summary>
        protected Consumer()
        {
            // Start the worker thread
            _worker = new Thread(Work);
            _worker.Start();
        }

        public void Dispose()
        {
            // Signal the consumer to exit.
            EnqueueTask(null, 0);
            // Wait for the consumer's thread to finish.
            _worker.Join();
            // Release any OS resources.
            _wh.Close();
        }

        /// <summary>
        ///     EnqueueTask - Queues a new data package for the consumer
        /// </summary>
        /// <param name="data">byte array of the data package</param>
        /// <param name="frameCounter">the index of the task/frame</param>
        public void EnqueueTask(byte[] data, long frameCounter)
        {
            lock (_locker)
            {
                _tasks.Enqueue(data);
            }

            _wh.Set();
        }

        /// <summary>
        ///     Work - Performs the consumer work cycle
        ///     - passes data to abstract DoWork function
        /// </summary>
        private void Work()
        {
            while (true)
            {
                byte[] data = null;
                lock (_locker)
                {
                    if (_tasks.Count > 0)
                    {
                        data = _tasks.Dequeue();
                        if (data == null) return;
                    }
                }

                if (data != null)
                {
                    DoWork(data);
                }
                else
                {
                    _wh.WaitOne(); // No more tasks - wait for a signal
                }
            }
        }

        /// <summary>
        ///     DoWork - abstract worker function
        ///     - handles the actual data processing as required
        /// </summary>
        /// <param name="data"></param>
        public abstract void DoWork(byte[] data);
    }
}
