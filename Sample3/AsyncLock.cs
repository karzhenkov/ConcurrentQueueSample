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
            private bool _completed;

            private Pretender() { }

            public static Pretender CreateCompleted()
            {
                return new Pretender { _task = Task.CompletedTask, _completed = true };
            }

            public static Pretender Create()
            {
                var tcs = new TaskCompletionSource<object>(null);
                return new Pretender { _tcs = tcs, _task = tcs.Task };
            }

            public async Task WaitAsync()
            {
                await _task;
                lock (this) if (_completed) return;
                await Task.Yield();
            }

            public void Dispose()
            {
                if (_tcs == null) return;

                lock (this)
                {
                    _tcs.SetResult(null);
                    _completed = true;
                }

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
