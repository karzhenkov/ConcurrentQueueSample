using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    class AsyncLock
    {
        private Pretender _last = Pretender.CreateCompleted();

        private class Pretender : IDisposable
        {
            private TaskCompletionSource<object> _tcs;
            private Task _task;
            private int? _disposingThreadId;

            private Pretender() { }

            public static Pretender CreateCompleted()
            {
                return new Pretender { _task = Task.CompletedTask };
            }

            public static Pretender Create()
            {
                var tcs = new TaskCompletionSource<object>(null);
                return new Pretender { _tcs = tcs, _task = tcs.Task };
            }

            public async Task WaitAsync()
            {
                await _task;
                if (_disposingThreadId != Thread.CurrentThread.ManagedThreadId) return;
                if (_tcs == null) return;
                await Task.Yield();
            }

            public void Dispose()
            {
                if (_tcs == null) return;
                _disposingThreadId = Thread.CurrentThread.ManagedThreadId;
                _tcs.SetResult(null);
                _tcs = null;
            }
        }

        public async Task<IDisposable> AcquireAsync()
        {
            var pretender = Pretender.Create();
            await Interlocked.Exchange(ref _last, pretender).WaitAsync();
            return pretender;
        }
    }
}
