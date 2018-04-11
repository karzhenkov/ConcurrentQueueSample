using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sample
{
    public abstract class AbstractDataSink<Item>
    {
        private Queue<Item> _queue = new Queue<Item>();
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
            lock (_queue)
            {
                _queue.Enqueue(item);
                if (_queue.Count != 1) return;
            }

            ProcessQueue();
        }

        private async void ProcessQueue()
        {
            await _task;
            await Task.Yield();

            var tcs = new TaskCompletionSource<object>();
            _task = tcs.Task;

            await OnProcessQueueEnter();

            while (true)
            {
                Item item;
                int count;

                lock (_queue)
                {
                    item = _queue.Dequeue();
                    count = _queue.Count;
                }

                await ProcessItem(item);
                if (count == 0) break;
            }

            await OnProcessQueueExit();
            tcs.SetResult(null);
        }
    }
}
