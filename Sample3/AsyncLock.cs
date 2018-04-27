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
            private TaskCompletionSource<int> _tcs;
            private Task<int> _task;

            private Pretender() { }

            public static Pretender CreateCompleted()
            {
                return new Pretender { _task = Task.FromResult(0) };
            }

            public static Pretender Create()
            {
                var tcs = new TaskCompletionSource<int>();
                return new Pretender { _tcs = tcs, _task = tcs.Task };
            }

            public async Task WaitAsync()
            {
                var disposingThreadId = await _task;
                if (disposingThreadId != Thread.CurrentThread.ManagedThreadId) return;
                if (_tcs == null) return;
                await Task.Yield();
            }

            public void Dispose()
            {
                if (_tcs == null) return;
                _tcs.SetResult(Thread.CurrentThread.ManagedThreadId);
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
