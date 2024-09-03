// See https://aka.ms/new-console-template for more information
namespace mg_1brc;

public interface IProcessFile
{
    ValueTask<Dictionary<string,Measurements>> ProcessFile(string filename);
}

