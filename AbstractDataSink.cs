using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sample
{
    public abstract class AbstractDataSink<Item>
    {
        private Queue<Item> _queue = new Queue<Item>();
        private Task _task = Task.CompletedTask;
        private bool _disabled;
        private TaskCompletionSource<object> _tcsDrain;

        protected abstract Task ProcessItem(Item item);

        protected virtual Task OnProcessQueueEnter()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnProcessQueueExit()
        {
            return Task.CompletedTask;
        }

        public bool Enqueue(Item item)
        {
            lock (_queue)
            {
                if (_disabled) return false;
                _queue.Enqueue(item);
                if (_queue.Count != 1) return true;
            }

            ProcessQueue();
            return true;
        }

        public Task DrainAsync()
        {
            lock (_queue)
            {
                _disabled = true;
                if (_queue.Count == 0) return _task;

                _tcsDrain = new TaskCompletionSource<object>();
                return _tcsDrain.Task;
            }
        }

        public void Drain()
        {
            try
            {
                DrainAsync().Wait();
            }
            catch (AggregateException e)
            {
                e.Handle(x => throw x);
            }
        }

        private async void ProcessQueue()
        {
            await _task;
            await Task.Yield();

            var tcs = new TaskCompletionSource<object>();
            _task = tcs.Task;

            TaskCompletionSource<object> tcsDrain;

            await OnProcessQueueEnter();

            while (true)
            {
                Item item;
                int count;

                lock (_queue)
                {
                    tcsDrain = _tcsDrain;
                    item = _queue.Dequeue();
                    count = _queue.Count;
                }

                await ProcessItem(item);
                if (count == 0) break;
            }

            await OnProcessQueueExit();
            tcs.SetResult(null);
            tcsDrain?.SetResult(null);
        }
    }
}
