using System.Threading;
using System.Threading.Tasks;

namespace Mailbox {
    sealed class Messenger<TMessage> {
        CancellationToken _cancellationToken;
        TaskCompletionSource<TMessage> _channel;

        public Messenger(TaskCompletionSource<TMessage> channel, CancellationToken cancel) {
            _channel = channel;
            _cancellationToken = cancel;
        }

        public bool IsCancelledOrTimedOut =>
            _cancellationToken.IsCancellationRequested;

        public void NotifyClient(TMessage message) =>
            _channel.SetResult(message);

        public void CancelClientRequest() =>
            _channel.SetCanceled();

        public bool TryNotifyOrCancelClient(TMessage message) {
            if(IsCancelledOrTimedOut) {
                CancelClientRequest();
                return false;
            }

            NotifyClient(message);
            return true;
        }

        public Task<TMessage> AsTask =>
            _channel.Task;
    }
}

