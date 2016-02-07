using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mailbox {
    public static class Channel {
        public static Channel<TMsg> For<TMsg>(DateTimeOffset created, TimeSpan timeout, CancellationToken cancel = default(CancellationToken)) =>
            new Channel<TMsg>(
                new TaskCompletionSource<TMsg>(),
                created,
                timeout,
                cancel);

        public static bool IsTimedOut<TMsg>(this Channel<TMsg> channel, DateTimeOffset timeToCheck) {
            return channel.Timeout > Timeout.InfiniteTimeSpan && timeToCheck - channel.Created >= channel.Timeout;
        }

        public static void Send<TMsg>(this Channel<TMsg> channel, TMsg msg) {
            channel.Remote.SetResult(msg);
        }

        public static bool TryCloseOnCancelledOrTimeout<TMsg>(this Channel<TMsg> channel, DateTimeOffset timeToCheck) {
            if(channel.IsTimedOut(timeToCheck)) {
                channel.Remote.SetException(new TimeoutException());
                return true;
            }

            if(channel.CancellationToken.IsCancellationRequested) {
                channel.Remote.SetCanceled();
                return true;
            }

            return false;
        }
    }

    public sealed class Channel<TMsg> {
        public CancellationToken CancellationToken { get; }
        public TaskCompletionSource<TMsg> Remote { get; }
        public DateTimeOffset Created { get; }
        public TimeSpan Timeout { get; }

        internal Channel(TaskCompletionSource<TMsg> remote, DateTimeOffset created, TimeSpan timeout, CancellationToken cancel = default(CancellationToken)) {
            Remote = remote;
            Created = created;
            Timeout = timeout;
            CancellationToken = cancel;
        }
    }
}

