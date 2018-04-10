using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    public abstract class AbstractDataSink<Item>
    {
        private ConcurrentQueue<Item> _queue = new ConcurrentQueue<Item>();
        private int _count;

        protected abstract Task ProcessItem(Item item);

        public void Enqueue(Item item)
        {
            _queue.Enqueue(item);
            if (Interlocked.Increment(ref _count) == 1) ProcessQueue();
        }

        private async void ProcessQueue()
        {
            do
            {
                Item item;
                _queue.TryDequeue(out item);
                await ProcessItem(item);
            } while (Interlocked.Decrement(ref _count) != 0);
        }
    }
}
