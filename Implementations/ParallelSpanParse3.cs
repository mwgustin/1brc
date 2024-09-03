
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

namespace mg_1brc;

public class ParallelSpanParse3 : IProcessFile
{
    public ParallelSpanParse3(int bufferSize = 10_000_000)
    {
        this.bufferSize = bufferSize;
    }

    //dictionary to hold partial lines that bridge chunks.
    //format is chunk as the index, a 2 element array to hold the beginning and end of the line, 
    // and a byte array to hold partial lines that bridge chunks
    Dictionary<int, byte[][]> leftovers = new Dictionary<int, byte[][]>();
    private int bufferSize {get; init;}
    public async ValueTask<Dictionary<string, Measurements>> ProcessFile(string filename)
    {
        Dictionary<string, Measurements> measurements = new Dictionary<string, Measurements>();
        using (var fileStream = new FileStream(filename, FileMode.Open))
        {
            //determine how many chunks we need to process based on bufferSize
            long Chunks = fileStream.Length / bufferSize;

            //init leftover dictionary to handle lines that bridge chunks
            leftovers = Enumerable.Range(0, (int)Chunks+1).ToDictionary(i => i, i => new byte[2][]);

            //task list that will process each chunk and retrun a dictionary of measurements
            List<ValueTask<int>> readTasks = new List<ValueTask<int>>();
            List<Task<Dictionary<string,Measurements>>> processTasks = new List<Task<Dictionary<string,Measurements>>>();
            // List<Func<Dictionary<string,Measurements>>> tasks = new List<Func<Dictionary<string,Measurements>>>();
            for(int i = 0; i <= Chunks; i++)
            {
                // var local = i;
                // processTasks.Add(Task.Run(async () => 
                // {
                //     byte[] byteBuffer= new byte[bufferSize];
                //     await fileStream.ReadAsync(byteBuffer);
                //     return ProcessChunk(byteBuffer, local);
                // }));
                var local = i;
                byte[] byteBuffer= new byte[bufferSize];
                await fileStream.ReadAsync(byteBuffer);
                processTasks.Add(Task.Run(() => 
                {
                    return ProcessChunk(byteBuffer, local);
                }));
                // tasks.Add(() => ProcessChunk(byteBuffer, local));
                // Console.WriteLine($"Chunk {i} - {byteBuffer.First()}");
            }
            

            //wait for all tasks to complete
            // ConcurrentBag<Dictionary<string, Measurements>> results = new ConcurrentBag<Dictionary<string, Measurements>>();
            // Parallel.ForEach(tasks, task => 
            // {
            //     results.Add(task());
            // });
            var results = await Task.WhenAll(processTasks);
            Console.WriteLine("All tasks complete");
            //handle leftover lines/beginning chunks
            var leftoverDict = ProcessLeftover();            
            var final = results.Append(leftoverDict);

            //reconcile chunk results
            foreach (var result in final)
            {
                foreach (var kvp in result)
                {
                    if (measurements.TryGetValue(kvp.Key, out var measurement))
                    {
                        measurement.Reconcile(kvp.Value);
                    }
                    else
                    {
                        measurements[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        return measurements;
    }
    private Dictionary<string, Measurements> ProcessChunk(Span<byte> buffer, int chunk)
    {
        Dictionary<string, Measurements> measurements = new Dictionary<string, Measurements>();
        var iterationLineCount = 0;
        while (buffer.Length > 0)
        {
            iterationLineCount++;
            var eolIndex = buffer.IndexOf((byte)'\n');
            var splitIndex = buffer.IndexOf((byte)';');
            //if we're on the first line of a chunk, we need handle a partial line from the previous chunk for later merging
            if(chunk > 0 && iterationLineCount == 1)
            {
                //parse off the first line of the chunk
                // we know a chunk index and array element already exist in leftoversl, so just slice the bits we need to reconcile later
                leftovers[chunk-1][1] = buffer.Slice(0, eolIndex).ToArray();
                
                //now that we've removed the partial line, update the buffer to the first full line and continue on
                buffer = buffer.Slice(eolIndex + 1);
                eolIndex = buffer.IndexOf((byte)'\n');
                splitIndex = buffer.IndexOf((byte)';');
            }
            //if we're at the end of the buffer, we need to handle the last line of the chunk for later merging
            if (eolIndex == -1)
            {
                //slice remaining buffer into the leftover dictionary for later merging
                leftovers[chunk][0] = buffer.ToArray();
                break;
            }

            //parse line and merge into dictionary
            var (stationName, measurementVal) = ParseLine(buffer, splitIndex, eolIndex);
            if(measurements.TryGetValue(stationName, out var measurement))
            {
                measurement.Add(measurementVal);
            }
            else
            {
                var m = new Measurements();
                m.Add(measurementVal);
                measurements[stationName] = m;
            }
            //update working buffer to remove the line we just parsed
            buffer = buffer.Slice(eolIndex + 1);
        }     
        return measurements;
    }

    private (string, double) ParseLine(ReadOnlySpan<byte> line, int splitIndex, int eolIndex) 
    {
        //convert span to utf8 string
        var stationName = Encoding.UTF8.GetString(line.Slice(0, splitIndex));
        //parse UTF8 value into resulting double.  
        Utf8Parser.TryParse(line.Slice(splitIndex + 1), out double measurementVal, out int _, 'f');
        return (stationName, measurementVal);
    }
    private Dictionary<string, Measurements> ProcessLeftover()
    {
        Dictionary<string, Measurements> measurements = new Dictionary<string, Measurements>();
        foreach(var kvp in leftovers)
        {
            var leftoverSpan = kvp.Value[0] ?? Array.Empty<byte>();
            var beginningSpan = kvp.Value[1] ?? Array.Empty<byte>();
            if(leftoverSpan.Length == 0 && beginningSpan.Length == 0)
            {
                continue;
            }
            var bridgedSpan = leftoverSpan.Concat(beginningSpan).ToArray();

            if(bridgedSpan.Length > 0)
            {
                var splitIndex = Array.IndexOf(bridgedSpan, (byte)';');
                var eolIndex = Array.IndexOf(bridgedSpan, (byte)'\n');

                if(splitIndex == -1 && eolIndex == -1)
                {
                    continue;
                }
                var (stationName, measurementVal) = ParseLine(bridgedSpan, splitIndex, eolIndex);
                if (measurements.TryGetValue(stationName, out var measurement))
                {
                    measurement.Add(measurementVal);
                }
                else
                {
                    var m = new Measurements();
                    m.Add(measurementVal);
                    measurements[stationName] = m;
                }
            }
        }
        return measurements;
    }


    private void PrintDictionary(Dictionary<string, Measurements> measurements)
    {
        foreach (var kvp in measurements)
        {
            Console.WriteLine($"{kvp.Key};{kvp.Value.Min};{kvp.Value.Avg};{kvp.Value.Max}");
        }
    }
}