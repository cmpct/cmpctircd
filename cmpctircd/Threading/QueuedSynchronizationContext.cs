using System;
using System.Threading;
using System.Collections.Concurrent;

namespace cmpctircd.Threading {
    public class QueuedSynchronizationContext : SynchronizationContext {
        // Message queue
        private BlockingCollection<CallbackStatePair> _queue = new BlockingCollection<CallbackStatePair>();
        // Queue lock (many producers, one consumer with "write" lock when completing adding)
        private ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

        /// <summary>
        /// Adds a message to the queue.
        /// </summary>
        /// <param name="pair">Callback and state pair message.</param>
        private void Add(CallbackStatePair pair) {
            _locker.EnterReadLock();
            _queue.Add(pair);
            _locker.ExitReadLock();
        }

        /// <summary>
        /// Dispatches an asynchronous message to the SynchronizationContext.
        /// </summary>
        /// <param name="d">The SendOrPostCallback delegate to call.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Post(SendOrPostCallback d, object state) {
            Add(new CallbackStatePair(d, state));
        }

        /// <summary>
        /// Dispatches a synchronous message to the SynchronizationContext.
        /// </summary>
        /// <param name="d">The SendOrPostCallback delegate to call.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Send(SendOrPostCallback d, object state) {
            ManualResetEventSlim reset = new ManualResetEventSlim();
            Add(new CallbackStatePair(d, state, reset));
            reset.Wait();
        }

        /// <summary>
        /// Processes the message queue.
        /// </summary>
        /// <param name="predicate">Predicate function which determines whether the processing should continue (true) or not (false).</param>
        private void Process(Func<bool> predicate) {
            while(predicate()) { // While the predicate returns true
                CallbackStatePair pair = _queue.Take(); // Take the next message
                pair.Delegate(pair.State); // Run the message
                pair.Reset?.Set(); // Set the event, if it exists
            }
        }

        /// <summary>
        /// Run the consumer of the message queue.
        /// </summary>
        /// <param name="predicate">Predicate function which determines whether the processing should continue (true) or not (false).</param>
        /// <returns>True if the consumer was run, False otherwise.</returns>
        public bool Run(Func<bool> predicate) {
            if(Monitor.TryEnter(_queue)) { // Mutual exclusion for only one processor
                if(_queue.IsAddingCompleted) { // If queue has already been completed and is unusuable
                    Monitor.Exit(_queue); // Unlock
                    return false;
                }
                Process(predicate); // Process the message queue until the predicate determines the stop
                _locker.EnterWriteLock(); // Lock to stop producers
                _queue.CompleteAdding(); // Notify adding completed
                _locker.ExitWriteLock(); // Unlock
                Process(() => !(_queue.IsCompleted)); // Process the message queue until it is empty
                Monitor.Exit(_queue); // Unlock
                return true;
            }
            return false;
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
    }
}