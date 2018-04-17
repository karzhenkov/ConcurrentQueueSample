using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sample
{
    public abstract class AbstractDataSink<Item>
    {
        [Flags]
        private enum Flags
        {
            Drain = 1,
            Break = 2
        }

        private Queue<Item> _queue = new Queue<Item>();
        private Flags _flags;
        private TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
        private Task _task;

        public AbstractDataSink()
        {
            _task = ProcessQueueAsync();
        }

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
                if (_flags != 0) return false;
                _queue.Enqueue(item);
                _tcs.TrySetResult(null);
                return true;
            }
        }

        public async Task DrainAsync()
        {
            lock (this)
            {
                _flags |= Flags.Drain;
                _tcs.TrySetResult(null);
            }

            await _task;
        }

        public void Break()
        {
            lock (this)
            {
                _flags |= Flags.Break;
                _tcs.TrySetResult(null);
            }

            try
            {
                Task.WhenAll(_task).Wait();
            }
            catch (AggregateException e)
            {
                e.Handle(x => throw x);
            }
        }

        private async Task ProcessQueueAsync()
        {
            while (true)
            {
                await _tcs.Task.ConfigureAwait(false);
                await Task.Yield();

                bool first = true;

                while (true)
                {
                    bool drainFlag;
                    bool hasItem = false;
                    var item = default(Item);

                    lock (_queue)
                    {
                        if (_flags.HasFlag(Flags.Break)) return;
                        drainFlag = _flags != 0;

                        if (_queue.Count == 0)
                        {
                            _tcs = new TaskCompletionSource<object>();
                        }
                        else
                        {
                            hasItem = true;
                            item = _queue.Dequeue();
                        }
                    }

                    if (!hasItem)
                    {
                        if (!first) await OnProcessQueueExit();
                        if (drainFlag) return;
                        break;
                    }

                    if (first)
                    {
                        first = false;
                        await OnProcessQueueEnter();
                    }

                    await ProcessItem(item);
                }
            }
        }
    }
}
