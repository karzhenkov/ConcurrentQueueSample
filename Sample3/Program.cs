using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    class DataSink : IDisposable
    {
        private StreamWriter _out;
        private readonly AsyncLock _asyncLock = new AsyncLock();

        public DataSink(string filename)
        {
            _out = File.CreateText(filename);
        }

        public async Task PutDataAsync(string item)
        {
            using (await _asyncLock.AcquireAsync())
            {
                Console.WriteLine(item);
                await _out.WriteLineAsync(item);
            }
        }

        public void Dispose()
        {
            if (_out == null) return;
            _out.Dispose();
            _out = null;
        }
    }

    class Program
    {
        static async Task GenerateItemsAsync(int id, DataSink sink, CancellationToken ct)
        {
            try
            {
                var random = new Random(id);

                while (true)
                {
                    int number = random.Next(1000);
                    await Task.Delay(TimeSpan.FromMilliseconds(number), ct);
                    await sink.PutDataAsync($"{id}: {number}");
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }

        static void Main(string[] args)
        {

            var cts = new CancellationTokenSource();
            using (var sink = new DataSink("sample.txt"))
            {
                var task = Task.WhenAll(from id in Enumerable.Range(0, 3) select GenerateItemsAsync(id, sink, cts.Token));
                Console.ReadLine();
                cts.Cancel();
                task.Wait();
            }
        }
    }
}
