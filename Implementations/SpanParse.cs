

using System;
using System.Buffers.Text;
using System.Text;

namespace mg_1brc;

public class SpanParse : IProcessFile
{
    private const int BufferSize = 1_000_000;
    public ValueTask<Dictionary<string, Measurements>> ProcessFile(string filename)
    {
        const int max_capacity = 10_000;
        Dictionary<string, Measurements> measurements = new Dictionary<string, Measurements>(max_capacity);
        using (var fileStream = new FileStream(filename, FileMode.Open))
        {
            var buffer = new Span<byte>(new byte[BufferSize]);
            Span<byte> leftoverBuffer = new Span<byte>(new byte[BufferSize]);
            int workingBufferBytes = 0;
            int bytesRead;
            int iteration = 0;
            int totalBytesRead = 0;
            while((bytesRead = fileStream.Read(buffer)) > 0)
            {
                totalBytesRead += bytesRead;
                iteration++;
                byte[] workingBytes = new byte[workingBufferBytes + buffer.Length];
                var workingBuffer = new Span<byte>(workingBytes);
                if(workingBufferBytes > 0)
                {
                    leftoverBuffer.Slice(0, workingBufferBytes).CopyTo(workingBytes);
                    buffer.CopyTo(workingBuffer.Slice(workingBufferBytes));
                }
                else
                {
                    workingBuffer = buffer;
                }
                var iterationLineCount = 0;
                while (workingBuffer.Length > 0)
                {
                    iterationLineCount++;
                    var eolIndex = workingBuffer.IndexOf((byte)'\n');
                    var splitIndex = workingBuffer.IndexOf((byte)';');
                    if (eolIndex == -1)
                    {
                        break;
                    }
                    // var line = Encoding.UTF8.GetString(workingBuffer.Slice(0, eolIndex));
                    var stationName = Encoding.UTF8.GetString(workingBuffer.Slice(0, splitIndex));
                    // var measurementVal = double.Parse(workingBuffer.Slice(splitIndex + 1, eolIndex - splitIndex - 1));
                    Utf8Parser.TryParse(workingBuffer.Slice(splitIndex + 1, eolIndex - splitIndex - 1), out double measurementVal, out int _, 'f');
                    // var measurementVal = BitConverter.ToDouble(workingBuffer.Slice(splitIndex + 1, eolIndex - splitIndex - 1));
                    // var values = line.Split(';');
                    // if(stationName == "dam")
                    // {
                    //     Console.WriteLine($"stationName: {stationName}, measurementVal: {measurementVal}. Iteration {iteration}. IterationLineCount {iterationLineCount}. Total bytes read: {totalBytesRead}. ");
                    // }
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
                    workingBuffer = workingBuffer.Slice(eolIndex + 1);
                }
                workingBuffer.CopyTo(leftoverBuffer);
                workingBufferBytes = workingBuffer.Length;
                if(iteration > 10_000)
                    buffer.Clear();
            }
        }
        return ValueTask.FromResult(measurements);
    }
}