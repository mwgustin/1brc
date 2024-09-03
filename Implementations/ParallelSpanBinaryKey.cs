
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

namespace mg_1brc;

//equal comparer for byte arrays to use as dict key.
// source: https://blog.codingmilitia.com/2023/12/06/byte-array-as-a-dictionary-key-trying-out-some-options/
public class ByteArrayEqualityComparerWithSpan : IEqualityComparer<byte[]>
{
    public bool Equals(byte[]? x, byte[]? y)
    => x is not null && y is not null 
        ? MemoryExtensions.SequenceEqual((ReadOnlySpan<byte>)x, (ReadOnlySpan<byte>)y)
        : x is null && y is null;

    public int GetHashCode(byte[] obj)
    {
        var hashCode = new HashCode();
        hashCode.AddBytes(obj);
        return hashCode.ToHashCode();
    }
}
public class ParallelSpanBinaryKey : IProcessFile
{
    private readonly IEqualityComparer<byte[]> _byteArrayComparer = new ByteArrayEqualityComparerWithSpan();
    private const byte _newline = (byte)'\n';
    private const byte _semicolon = (byte)';';
    public ParallelSpanBinaryKey(int bufferSize = 10_000_000)
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
        using var fileStream = new FileStream(filename, FileMode.Open);
        
        //determine how many chunks we need to process based on bufferSize
        long Chunks = fileStream.Length / bufferSize;

        //init leftover dictionary to handle lines that bridge chunks
        leftovers = Enumerable.Range(0, (int)Chunks+1).ToDictionary(i => i, i => new byte[2][]);

        //task list that will process each chunk and retrun a dictionary of measurements
        List<ValueTask<int>> readTasks = new List<ValueTask<int>>();
        List<Task<Dictionary<byte[],Measurements>>> processTasks = new List<Task<Dictionary<byte[],Measurements>>>();
        for(int i = 0; i <= Chunks; i++)
        {
            var local = i;
            byte[] byteBuffer= new byte[bufferSize];
            fileStream.Read(byteBuffer);
            processTasks.Add(Task.Run(() => 
            {
                return ProcessChunk(byteBuffer, local);
            }));
        }
        

        //wait for all tasks to complete
        var tempResults = await Task.WhenAll(processTasks);
        Console.WriteLine("All tasks complete");
        //handle leftover lines/beginning chunks
        var leftoverDict = ProcessLeftover();       
        tempResults.Append(leftoverDict);

        //reconcile chunk results
        Dictionary<byte[], Measurements> finalMeasurements = new Dictionary<byte[], Measurements>(_byteArrayComparer);
        foreach (var result in tempResults)
        {
            foreach (var kvp in result)
            {
                if (finalMeasurements.TryGetValue(kvp.Key, out var measurement))
                {
                    measurement.Reconcile(kvp.Value);
                }
                else
                {
                    finalMeasurements[kvp.Key] = kvp.Value;
                }
            }
        }
        return finalMeasurements.ToDictionary(x => Encoding.UTF8.GetString(x.Key), x => x.Value);
    }
    private Dictionary<byte[], Measurements> ProcessChunk(Span<byte> buffer, int chunk)
    {
        Dictionary<byte[], Measurements> measurements = new Dictionary<byte[], Measurements>(_byteArrayComparer);
        var iterationLineCount = 0;
        while (buffer.Length > 0)
        {
            iterationLineCount++;
            var eolIndex = buffer.IndexOf(_newline);
            var splitIndex = buffer.IndexOf(_semicolon);
            //if we're on the first line of a chunk, we need handle a partial line from the previous chunk for later merging
            if(chunk > 0 && iterationLineCount == 1)
            {
                //parse off the first line of the chunk
                // we know a chunk index and array element already exist in leftoversl, so just slice the bits we need to reconcile later
                leftovers[chunk-1][1] = buffer.Slice(0, eolIndex).ToArray();
                
                //now that we've removed the partial line, update the buffer to the first full line and continue on
                buffer = buffer.Slice(eolIndex + 1);
                eolIndex = buffer.IndexOf(_newline);
                splitIndex = buffer.IndexOf(_semicolon);
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

    // private (string, double) ParseLine(ReadOnlySpan<byte> line, int splitIndex, int eolIndex) 
    // {
    //     //convert span to utf8 string
    //     var stationName = Encoding.UTF8.GetString(line.Slice(0, splitIndex));
    //     //parse UTF8 value into resulting double.  
    //     Utf8Parser.TryParse(line.Slice(splitIndex + 1), out double measurementVal, out int _, 'f');
    //     return (stationName, measurementVal);
    // }
    private (byte[], double) ParseLine(ReadOnlySpan<byte> line, int splitIndex, int eolIndex) 
    {
        //convert span to utf8 string
        var stationName = line.Slice(0, splitIndex);
        //parse UTF8 value into resulting double.  
        Utf8Parser.TryParse(line.Slice(splitIndex + 1), out double measurementVal, out int _, 'f');
        return (stationName.ToArray(), measurementVal);
    }
    private Dictionary<byte[], Measurements> ProcessLeftover()
    {
        Dictionary<byte[], Measurements> measurements = new Dictionary<byte[], Measurements>( _byteArrayComparer);
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
                var splitIndex = Array.IndexOf(bridgedSpan, _semicolon);
                var eolIndex = Array.IndexOf(bridgedSpan, _newline);

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