using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace Mailbox {
    public sealed class Mailbox<TMsg> {
        readonly ConcurrentQueue<TMsg> _inbox;
        readonly ConcurrentQueue<Messenger<TMsg>> _messengers;

        public Mailbox() {
            _inbox = new ConcurrentQueue<TMsg>();
            _messengers = new ConcurrentQueue<Messenger<TMsg>>();
        }

        public void Post(TMsg msg) {
            Messenger<TMsg> messenger;
            while(_messengers.TryDequeue(out messenger)) {
                if(messenger.TryNotifyOrCancelClient(msg))
                    return;
            }

            _inbox.Enqueue(msg);
        }

        public Task<TMsg> ReceiveAsync(TimeSpan timeout) =>
            ReceiveAsync(timeout, CancellationToken.None);

        public Task<TMsg> ReceiveAsync(CancellationToken cancel = default(CancellationToken)) =>
            ReceiveAsync(Timeout.InfiniteTimeSpan, cancel);

        public Task<TMsg> ReceiveAsync(TimeSpan timeout, CancellationToken cancel = default(CancellationToken)) {
            if(timeout < Timeout.InfiniteTimeSpan || timeout.TotalMilliseconds > int.MaxValue)
                throw new ArgumentOutOfRangeException($"The timeout ({timeout}) must be not be smaller than -1 ms or larger than Int32.MaxValue ms.", nameof(timeout));

            var messenger = CreateMessenger(timeout, cancel);
            if(messenger.IsCancelledOrTimedOut) {
                messenger.CancelClientRequest();
            } else {
                NotifyIfMessageAvailableOrEnqueue(messenger);
            }

            return messenger.AsTask;
        }

        Messenger<TMsg> CreateMessenger(TimeSpan timeout, CancellationToken cancel) {
            var tcs = new TaskCompletionSource<TMsg>();
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            cts.CancelAfter(timeout);
            cts.Token.Register(() => cts.Dispose());

            return new Messenger<TMsg>(tcs, cts.Token);
        }

        void NotifyIfMessageAvailableOrEnqueue(Messenger<TMsg> consumer) {
            TMsg msg;
            if(_inbox.TryDequeue(out msg))
                consumer.NotifyClient(msg);
            else
                _messengers.Enqueue(consumer);
        }
    }
}

