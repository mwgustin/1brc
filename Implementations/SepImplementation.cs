// using System.Collections.Concurrent;
// using CommandLine;
// using mg_1brc;
// using nietras.SeparatedValues;

// public class ParallelSepImplementation : IProcessFile
// {
//     public ValueTask<Dictionary<string, Measurements>> ProcessFile(string filename)
//     {
//         Dictionary<string, Measurements> measurements = new Dictionary<string, Measurements>(10_000);
//         var reader = Sep.New(';').Reader(opts => opts with { HasHeader = false}).FromFile(filename);
//         var results = reader.ParallelEnumerate(ParseRow);
//             // .AsParallel().GroupBy(x => x.name).ForAll(group =>
//             // {
//             //     var measurement = new Measurements();
//             //     foreach (var importMeasure in group)
//             //     {
//             //         measurement.Add(importMeasure.value);
//             //     }
//             //     measurements[group.Key] = measurement;
//             // });
//         results.GroupBy(x => x.name).AsParallel().ForAll(group =>
//         {
//             var measurement = new Measurements();
//             foreach (var importMeasure in group)
//             {
//                 measurement.Add(importMeasure.value);
//             }
//             measurements[group.Key] = measurement;
//         });
//         return ValueTask.FromResult(measurements);
//     }
//     private ImportMeasure ParseRow(SepReader.Row row)
//     {
//         return new ImportMeasure(row[0].Parse<string>(), row[1].Parse<double>());
//     }
// }
// public class SepImplementation : IProcessFile
// {
//     public ValueTask<Dictionary<string, Measurements>> ProcessFile(string filename)
//     {
//         var reader = Sep.New(';').Reader(opts => opts with { HasHeader = false}).FromFile(filename);
//         Dictionary<string, Measurements> measurements = new Dictionary<string, Measurements>(10_000);
        
        
//         foreach (var item in reader)
//         {
//             // // Console.WriteLine($"{item[0].Parse<string>()} - {item[1].Parse<double>()}" );
//             var name = item[0].Parse<string>();
//             var val = item[1].Parse<double>();
//             if(measurements.TryGetValue(name, out var measurement))
//             {
//                 measurement.Add(val);
//             }
//             else
//             {
//                 var m = new Measurements();
//                 m.Add(val);
//                 measurements[name] = m;
//             }
//         }
        
//         return ValueTask.FromResult(measurements);
//     }

    
// }