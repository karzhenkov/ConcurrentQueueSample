using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    public abstract class AbstractDataSink<Item>
    {
        private ConcurrentQueue<Item> _queue = new ConcurrentQueue<Item>();
        private int _count;
        private TaskCompletionSource<object> _tcs;
        private Task _task = Task.FromResult<object>(null);

        protected abstract Task ProcessItem(Item item);

        protected virtual Task OnProcessQueueEnter()
        {
            return Task.FromResult<object>(null);
        }

        protected virtual Task OnProcessQueueExit()
        {
            return Task.FromResult<object>(null);
        }

        public void Enqueue(Item item)
        {
            _queue.Enqueue(item);
            if (Interlocked.Increment(ref _count) == 1) Task.Run(async () =>
            {
                await _task;
                _tcs = new TaskCompletionSource<object>(null);
                _task = _tcs.Task;
                ProcessQueue();
            });
        }

        private async void ProcessQueue()
        {
            await OnProcessQueueEnter();

            do
            {
                Item item;
                _queue.TryDequeue(out item);
                await ProcessItem(item);
            } while (Interlocked.Decrement(ref _count) != 0);

            await OnProcessQueueExit();
            _tcs.SetResult(null);
        }
    }
}
