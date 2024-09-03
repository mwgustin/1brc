// // See https://aka.ms/new-console-template for more information
// using System.ComponentModel.DataAnnotations.Schema;
// using System.Threading.Tasks.Dataflow;
// using BenchmarkDotNet.Attributes;
// using BenchmarkDotNet.Configs;
// using BenchmarkDotNet.Environments;
// using BenchmarkDotNet.Jobs;

// namespace mg_1brc;

// public class BenchmarkConfig : ManualConfig
// {
//     public BenchmarkConfig()
//     {
//         Add(DefaultConfig.Instance);
//         AddJob(
//             Job.Default
//             .WithRuntime(NativeAotRuntime.Net80)
//             .WithLaunchCount(1)
//             .WithIterationCount(3)
//             .WithWarmupCount(1)
//         );
//         AddJob(
//             Job.Default
//             .WithRuntime(CoreRuntime.Core80)
//             .WithLaunchCount(1)
//             .WithIterationCount(3)
//             .WithWarmupCount(1)
//         );
//     }
// }
// [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
// // [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.NativeAot80)]
// [MemoryDiagnoser]
// [MinColumn, MaxColumn, MeanColumn, MedianColumn, RankColumn]
// public class BenchmarkTester
// {
//     // private const string fileName = "/Users/mgustin/Dev/playground/1brc/mg_1brc/data/measurements-100000.txt";
//     // private const string fileName = "/Users/mgustin/Dev/playground/1brc/mg_1brc/data/measurements-10000000.txt";
//     private const string fileName = "/Users/mgustin/Dev/playground/1brc/mg_1brc/data/measurements.txt";
//     private readonly IProcessFile naive = new NaiveImplementation();
//     private readonly IProcessFile naive2 = new NaiveImplementation2();
//     private readonly IProcessFile span = new SpanParse();
//     private readonly IProcessFile pSpan = new ParallelSpanParse();
//     private readonly IProcessFile pSpan2 = new ParallelSpanParse2();
//     private readonly IProcessFile pSpan3 = new ParallelSpanParse3();
//     private readonly IProcessFile pSpanByteDict = new ParallelSpanBinaryKey();
//     private readonly IProcessFile pSpanByteDictManualParse = new ParallelSpanBinaryKeyManualParse();
//     private readonly IProcessFile channel = new ChannelImplementation();
//     // private readonly IProcessFile parallelChannel = new ParallelChannelImplementation();
//     private readonly IProcessFile memMap = new MMapImplementation();
//     // private readonly IProcessFile sep = new SepImplementation();
//     // private readonly IProcessFile parallelSep = new ParallelSepImplementation();
//     // private readonly IProcessFile df = new DataFlowImplementation();
//     // private readonly IProcessFile df2 = new DataFlowImplementation(parallel: 10);

//     // [Benchmark]
//     // public ValueTask<Dictionary<string, Measurements>> NaiveTest()  => naive.ProcessFile(fileName);
//     // [Benchmark]
//     // public ValueTask<Dictionary<string, Measurements>> NaiveTest2()  => naive2.ProcessFile(fileName);
//     // [Benchmark]
//     // public ValueTask<Dictionary<string, Measurements>> Span()  => span.ProcessFile(fileName);
//     // [Benchmark]
//     // public ValueTask<Dictionary<string, Measurements>> ParallelSpan()  => pSpan.ProcessFile(fileName);
//     [Benchmark]
//     public ValueTask<Dictionary<string, Measurements>> ParallelSpanByteDict()  => pSpanByteDict.ProcessFile(fileName);
//     [Benchmark]
//     public ValueTask<Dictionary<string, Measurements>> ParallelSpanByteDictManualParse()  => pSpanByteDictManualParse.ProcessFile(fileName);
//     // [Benchmark]
//     // public ValueTask<Dictionary<string, Measurements>> ParallelSpan2()  => pSpan2.ProcessFile(fileName);
//     // [Benchmark]
//     // public ValueTask<Dictionary<string, Measurements>> ParallelSpan3()  => pSpan3.ProcessFile(fileName);
//     // [Benchmark]
//     // public ValueTask<Dictionary<string, Measurements>> Binary4()  => pSpan4.ProcessFile(fileName);
//     // [Benchmark]
//     // public ValueTask<Dictionary<string, Measurements>> ChannelTest()  => channel.ProcessFile(fileName);
//     // [Benchmark]
//     // public ValueTask<Dictionary<string, Measurements>> ParallelChannelTest()  => parallelChannel.ProcessFile(fileName);
//     // [Benchmark]
//     // public ValueTask<Dictionary<string, Measurements>> MMapTest()  => memMap.ProcessFile(fileName);
//     // [Benchmark]
//     // public ValueTask<Dictionary<string, Measurements>> SepTest()  => sep.ProcessFile(fileName);
//     // [Benchmark]
//     // public ValueTask<Dictionary<string, Measurements>> ParallelSepTest()  => parallelSep.ProcessFile(fileName);
//     // [Benchmark]
//     // public ValueTask<Dictionary<string, Measurements>> DfTest()  => df.ProcessFile(fileName);
//     // [Benchmark]
//     // public ValueTask<Dictionary<string, Measurements>> DfTest2()  => df2.ProcessFile(fileName);

// }