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

                while (true)
                {
                    Item item;

                    lock (_queue)
                    {
                        if (_flags.HasFlag(Flags.Break)) return;

                        if (_queue.Count == 0)
                        {
                            if (_flags != 0) return;
                            _tcs = new TaskCompletionSource<object>();
                            break;
                        }

                        item = _queue.Dequeue();
                    }

                    await ProcessItem(item);
                }
            }
        }
    }
}
