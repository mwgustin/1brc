using System.Collections.Concurrent;
using System.Threading.Channels;

namespace mg_1brc;
public class ChannelImplementation : IProcessFile
{
    public async ValueTask<Dictionary<string, Measurements>> ProcessFile(string filename)
    {
        var finalMeasurements = new ConcurrentDictionary<string, Measurements>();
        var channel = Channel.CreateUnbounded<string>();
        
        _ = Task.Run(async () => 
        {
            var lines = File.ReadLines(filename);
            foreach (var line in lines)
            {
                await channel.Writer.WriteAsync(line);
            }
            channel.Writer.Complete();
        });

        await foreach (var line in channel.Reader.ReadAllAsync())
        {
            var values = line.Split(';');
            if(finalMeasurements.TryGetValue(values[0], out var measurement))
            {
                measurement.Add(double.Parse(values[1]));
            }
            else
            {
                var m = new Measurements();
                m.Add(double.Parse(values[1]));
                finalMeasurements[values[0]] = m;
            }
        }

        return finalMeasurements.ToDictionary();
    }
}