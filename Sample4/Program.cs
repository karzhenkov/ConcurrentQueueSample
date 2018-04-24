using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace Sample
{
    class DataSink : IDisposable
    {
        private StreamWriter _out;

        public DataSink(string filename)
        {
            _out = File.CreateText(filename);
        }

        public void PutData(string item)
        {
            Console.WriteLine(item);
            _out.WriteLine(item);
        }

        public void Dispose()
        {
            _out.Dispose();
        }
    }

    class Program
    {
        static async Task GenerateAsync(IObserver<string> observer, int id, CancellationToken ct)
        {
            var random = new Random(id);

            while (true)
            {
                int number = random.Next(1000);
                await Task.Delay(TimeSpan.FromMilliseconds(number), ct);
                observer.OnNext($"{id}: {number}");
            }
        }

        static async Task Test(CancellationToken ct)
        {
            using (var sink = new DataSink("sample.txt"))
            {
                var sources =
                    from id in Enumerable.Range(0, 3)
                    select Observable.Create<string>(
                        observer => GenerateAsync(observer, id, ct));

                var tcs = new TaskCompletionSource<object>();

                Observable
                    .Merge(sources)
                    .Finally(() => tcs.SetResult(null))
                    .Subscribe(sink.PutData);

                await tcs.Task;
            }
        }

        static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            var task = Test(cts.Token);
            Console.ReadLine();
            cts.Cancel();
            task.Wait();
        }
    }
}
