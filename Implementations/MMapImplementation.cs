// See https://aka.ms/new-console-template for more information
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;

namespace mg_1brc;

public class MMapImplementation : IProcessFile
{
    public ValueTask<Dictionary<string, Measurements>> ProcessFile(string filename)
    {
        var finalMeasurements = new ConcurrentDictionary<string, Measurements>();
        var memoryMappedFile = MemoryMappedFile.CreateFromFile(filename, FileMode.Open);
        using (var memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor())
        {
            // var chunkSize = int.MaxValue / 90000000;
            var NumberOfChunks = 1000;

            // long offset = 0;
            long chunks = memoryMappedViewAccessor.Capacity / NumberOfChunks;
            Parallel.ForEach(Enumerable.Range(0, NumberOfChunks), (i) =>
            {
                Dictionary<string, Measurements> measurements = new Dictionary<string, Measurements>(10_000);
                var localLineCount = 0;
                byte[] buffer = new byte[chunks];
                memoryMappedViewAccessor.ReadArray(i*chunks, buffer, 0, buffer.Length);
                var lines = System.Text.Encoding.UTF8.GetString(buffer).Split('\n');
                for (int i1 = 1; i1 < lines.Length-1; i1++)
                {
                    string? line = lines[i1];
                    localLineCount++;
                    var values = line.Split(';');
                    if(values.Count() < 2) continue;
                    if(measurements.TryGetValue(values[0], out var measurement))
                    {
                        measurement.Add(double.Parse(values[1]));
                    }
                    else
                    {
                        var m = new Measurements();
                        m.Add(double.Parse(values[1]));
                        measurements[values[0]] = m;
                    }
                }
                foreach(var measurement in measurements)
                {
                    finalMeasurements.AddOrUpdate(measurement.Key, measurement.Value, (key, existing) => {
                        existing.Reconcile(measurement.Value);
                        return existing;
                    });
                }
            });
        }
        return ValueTask.FromResult(finalMeasurements.ToDictionary());
    }
}