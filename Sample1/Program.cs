using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    class DataSink : AbstractDataSink<string>
    {
        protected override async Task ProcessItem(string item)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            await Console.Out.WriteLineAsync(item);
        }
    }

    class Program
    {
        static async void GenerateItemsAsync(int id, DataSink sink)
        {
            var random = new Random(id);

            while (true)
            {
                int number = random.Next(1000);
                await Task.Delay(TimeSpan.FromMilliseconds(number));
                sink.Enqueue($"{id}: {number}");
            }
        }

        static void Main(string[] args)
        {
            var sink = new DataSink();
            for (int i = 0; i < 3; i++) GenerateItemsAsync(i, sink);
            Console.ReadLine();
        }
    }
}
