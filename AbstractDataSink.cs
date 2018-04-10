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
        private Task _task = Task.CompletedTask;

        protected abstract Task ProcessItem(Item item);

        protected virtual Task OnProcessQueueEnter()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnProcessQueueExit()
        {
            return Task.CompletedTask;
        }

        public void Enqueue(Item item)
        {
            _queue.Enqueue(item);
            if (Interlocked.Increment(ref _count) == 1) Task.Run((Action)ProcessQueue);
        }

        private async void ProcessQueue()
        {
            await _task;

            var tcs = new TaskCompletionSource<object>();
            _task = tcs.Task;

            await OnProcessQueueEnter();

            do
            {
                Item item;
                _queue.TryDequeue(out item);
                await ProcessItem(item);
            } while (Interlocked.Decrement(ref _count) != 0);

            await OnProcessQueueExit();
            tcs.SetResult(null);
        }
    }
}
