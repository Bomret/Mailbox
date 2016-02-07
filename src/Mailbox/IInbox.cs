using System;
using System.Threading.Tasks;
using System.Threading;

namespace Mailbox {
    public interface IInbox<TMsg> {
        int MessageCount { get; }

        void Post(TMsg message);

        Task<TMsg> ReceiveAsync(TimeSpan timeout);
        Task<TMsg> ReceiveAsync(CancellationToken cancel = default(CancellationToken));
        Task<TMsg> ReceiveAsync(TimeSpan timeout, CancellationToken cancel = default(CancellationToken));
    }
}
