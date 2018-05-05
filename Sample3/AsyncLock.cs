using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    class AsyncLock
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private class Releaser : IDisposable
        {
            private SemaphoreSlim _semaphore;

            public Releaser(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                if (_semaphore == null) return;
                _semaphore.Release();
                _semaphore = null;
            }
        }

        public async Task<IDisposable> AcquireAsync()
        {
            await _semaphore.WaitAsync();
            return new Releaser(_semaphore);
        }
    }
}
