using System.Runtime.CompilerServices;

namespace mg_1brc;

public class NaiveImplementation : IProcessFile
{
    
    public ValueTask<Dictionary<string, Measurements>> ProcessFile(string filename)
    {
        const int max_capacity = 10_000;
        Dictionary<string, Measurements> measurements = new Dictionary<string, Measurements>(max_capacity);
        using (var fileStream = new FileStream(filename, FileMode.Open))
        using (var bufferedStream = new BufferedStream(fileStream))
        using (var streamReader = new StreamReader(bufferedStream))
        {
            string line;
            string[] lineBuffer = new string[1000];
            while ((line = streamReader.ReadLine()) != null)
            {
                var values = line.Split(';');
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
        }
        return ValueTask.FromResult(measurements);
    }
}
