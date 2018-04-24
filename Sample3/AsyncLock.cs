using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    class AsyncLock
    {
        private Task _task = Task.CompletedTask;

        private class Pretender : IDisposable
        {
            private TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

            public Task Task => _tcs.Task;

            public void Dispose()
            {
                _tcs.SetResult(null);
            }
        }

        public async Task<IDisposable> AcquireAsync()
        {
            var pretender = new Pretender();
            var task = Interlocked.Exchange(ref _task, pretender.Task);
            
            if (!task.IsCompleted)
            {
                await task;
                await Task.Yield();
            }

            return pretender;
        }
    }
}
