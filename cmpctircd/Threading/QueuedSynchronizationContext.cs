using System;
using System.Threading;
using System.Collections.Concurrent;

namespace cmpctircd.Threading {
    public class QueuedSynchronizationContext : SynchronizationContext, IDisposable {
        // Message queue
        private readonly BlockingCollection<CallbackStatePair> _queue = new BlockingCollection<CallbackStatePair>();

        /// <summary>
        /// Adds a message to the queue.
        /// </summary>
        /// <param name="pair">Callback and state pair message.</param>
        private void Add(CallbackStatePair pair) {
            _queue.Add(pair);
        }

        /// <summary>
        /// Dispatches an asynchronous message to the SynchronizationContext.
        /// </summary>
        /// <param name="d">The SendOrPostCallback delegate to call.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Post(SendOrPostCallback d, object state) {
            if(d == null)
                throw new ArgumentNullException(nameof(d));
            Add(new CallbackStatePair(d, state));
        }

        /// <summary>
        /// Dispatches a synchronous message to the SynchronizationContext.
        /// </summary>
        /// <param name="d">The SendOrPostCallback delegate to call.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Send(SendOrPostCallback d, object state) {
            if(d == null)
                throw new ArgumentNullException(nameof(d));
            using (ManualResetEventSlim reset = new ManualResetEventSlim()) {
                Add(new CallbackStatePair(d, state, reset));
                reset.Wait();
            }
        }

        /// <summary>
        /// Processes the message queue.
        /// </summary>
        private void Process() {
            while(!_queue.IsCompleted) { // While the queue is not completed
                CallbackStatePair pair = _queue.Take(); // Take the next message
                if(pair.Delegate == null) { // If the delegate is null, complete adding
                    _queue.CompleteAdding(); // Notify adding completed
                } else {
                    pair.Delegate(pair.State); // Run the message
                    pair.Reset?.Set(); // Set the event, if it exists
                }
            }
        }

        /// <summary>
        /// Run the consumer of the message queue.
        /// </summary>
        /// <returns>True if the consumer was run, False otherwise.</returns>
        public bool Run() {
            if(Monitor.TryEnter(_queue)) { // Mutual exclusion for only one processor
                if(_queue.IsCompleted) { // If queue has already been completed and is unusuable
                    Monitor.Exit(_queue); // Unlock
                    return false;
                }
                Process(); // Process the message queue until the predicate determines the stop
                Monitor.Exit(_queue); // Unlock
                return true;
            }
            return false;
        }

        public void Stop() {
            Add(new CallbackStatePair(null, null));
        }

        private struct CallbackStatePair {
            /// <summary>
            /// The SendOrPostCallback delegate to call.
            /// </summary>
            public SendOrPostCallback Delegate { get; private set; }
            /// <summary>
            /// The object passed to the delegate.
            /// </summary>
            public object State { get; private set; }
            /// <summary>
            /// The reset event for synchronous dispatch.
            /// </summary>
            public ManualResetEventSlim Reset { get; private set; }

            /// <summary>
            /// CallbackStatePairConstructor
            /// </summary>
            /// <param name="d">The SendOrPostCallback delegate to call.</param>
            /// <param name="state">The object passed to the delegate.</param>
            /// <param name="reset">The reset event for synchronous dispatch.</param>
            public CallbackStatePair(SendOrPostCallback d, object state, ManualResetEventSlim reset = null) {
                Delegate = d;
                State = state;
                Reset = reset;
            }
        }

        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposed) {
                if (disposing) {
                    _queue.Dispose();
                }
                disposed = true;
            }
        }

        /// <summary>
        /// Dispose of the object, releasing resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
        }
        #endregion
    }
}