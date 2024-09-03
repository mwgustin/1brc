// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
// using BenchmarkDotNet.Running;

namespace mg_1brc;

internal class Program
{
    private static async Task Main(string[] args)
    {
        
        // BenchmarkRunner.Run(typeof(Program).Assembly);
        // BenchmarkRunner.Run(typeof(Program).Assembly, new BenchmarkConfig());
        await Execute(args);
        
    }

    private static async Task Execute(string[] args)
    {
        // Console.WriteLine("Starting!");
        // Stopwatch sw = new Stopwatch();
        // sw.Start();
        // var filename = args.Length > 0 ? args[0] : "data/measurements-100000.txt";
        var filename = args.Length > 0 ? args[0] : "data/measurements.txt";
        // var test =  new NaiveImplementation();
        // var test =  new NaiveImplementation2();
        // var test =  new MMapImplementation();
        // var test =  new ChannelImplementation();
        // var test =  new SepImplementation();
        // var test =  new DataFlowImplementation();
        // var test =  new SpanParse();
        // var test = new ParallelSpanParse();
        // var test = new ParallelSpanNoAsyncParse();
        // var test = new ParallelSpanBinaryKey();
        var test = new ParallelSpanBinaryKeyManualParse();
        var finalMeasurements = await test.ProcessFile(filename);


        using (var fileStream = File.Create("output.txt"))
        {
            using (var streamWriter = new StreamWriter(fileStream))
            {
                foreach (var measurement in finalMeasurements)
                {
                    streamWriter.WriteLine($"{measurement.Key};{measurement.Value.Min};{measurement.Value.Avg};{measurement.Value.Max}");
                    // streamWriter.WriteLine($"{measurement.Key};{measurement.Value.Min};{measurement.Value.Avg};{measurement.Value.Max} - {measurement.Value.Count}");
                }
            }
        }



        // sw.Stop();
        // Console.WriteLine("=====================");
        // Console.WriteLine($"Time taken: {sw.Elapsed}");
    }
}
