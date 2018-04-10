using System;
using System.IO;
using System.Threading.Tasks;

namespace Sample
{
    class DataSink : AbstractDataSink<string>, IDisposable
    {
        private StreamWriter _out;

        public DataSink(string filename)
        {
            _out = File.CreateText(filename);
        }

        protected override async Task ProcessItem(string item)
        {
            Console.WriteLine(item);
            await _out.WriteLineAsync(item);
        }

        protected override Task OnProcessQueueExit()
        {
            return _out.FlushAsync();
        }

        public void Dispose()
        {
            _out.Dispose();
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
            using (var sink = new DataSink("sample.txt"))
            {
                for (int i = 0; i < 3; i++) GenerateItemsAsync(i, sink);
                Console.ReadLine();
            }
        }
    }
}
