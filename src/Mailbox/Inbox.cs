using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace Mailbox {
    public sealed class Inbox<TMsg> : IInbox<TMsg> {
        readonly ConcurrentQueue<TMsg> _inbox;
        readonly ConcurrentQueue<Channel<TMsg>> _channels;

        public int MessageCount => _inbox.Count;

        public Inbox() {
            _inbox = new ConcurrentQueue<TMsg>();
            _channels = new ConcurrentQueue<Channel<TMsg>>();
        }

        public void Post(TMsg message) =>
            NotifyIfChannelAvailableOrEnqueue(message);

        public Task<TMsg> ReceiveAsync(TimeSpan timeout) =>
            ReceiveAsync(timeout, CancellationToken.None);

        public Task<TMsg> ReceiveAsync(CancellationToken cancel = default(CancellationToken)) =>
            ReceiveAsync(Timeout.InfiniteTimeSpan, cancel);

        public Task<TMsg> ReceiveAsync(TimeSpan timeout, CancellationToken cancel = default(CancellationToken)) {
            if(timeout < Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException($"The timeout ({timeout}) must be not be smaller than -1 ms.", nameof(timeout));

            var utcNow = DateTimeOffset.UtcNow;
            var channel = Channel.For<TMsg>(utcNow, timeout, cancel);

            NotifyIfMessageAvailableOrEnqueue(channel);

            return channel.Remote.Task;
        }

        void NotifyIfChannelAvailableOrEnqueue(TMsg message) {
            Channel<TMsg> channel;
            /* Tries to send the msg to the first open channel.
             * Dequeues cancelled or timed out channels until it finds
             * an open one or empties the queue.
             */
            while(_channels.TryDequeue(out channel)) {
                if(channel.TryCloseOnCancelledOrTimeout(DateTimeOffset.UtcNow))
                    continue;

                channel.Send(message);
                return;
            }

            _inbox.Enqueue(message);
        }

        void NotifyIfMessageAvailableOrEnqueue(Channel<TMsg> channel) {
            if(channel.TryCloseOnCancelledOrTimeout(DateTimeOffset.UtcNow)) {
                return;
            }

            TMsg msg;
            if(_inbox.TryDequeue(out msg))
                channel.Send(msg);
            else
                _channels.Enqueue(channel);
        }
    }
}