using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sample
{
    public abstract class AbstractDataSink<Item>
    {
        // Режимы завершения цикла обработки элементов
        private enum StopMode
        {
            None,  // не завершать цикл
            Drain, // обработать имеющиеся элементы
            Break  // завершить без обработки имеющихся элементов
        }

        private Queue<Item> _queue = new Queue<Item>();
        private StopMode _stopMode = StopMode.None;
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
                if (_stopMode != StopMode.None) return false;
                _queue.Enqueue(item);
                _tcs.TrySetResult(null);
                return true;
            }
        }

        public async Task DrainAsync()
        {
            lock (this)
            {
                if (_stopMode == StopMode.None) _stopMode = StopMode.Drain;
                _tcs.TrySetResult(null);
            }

            await _task;
        }

        public void Break()
        {
            lock (this)
            {
                _stopMode = StopMode.Break;
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
                        if (_stopMode == StopMode.Break) return;

                        if (_queue.Count == 0)
                        {
                            if (_stopMode == StopMode.Drain) return;
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
