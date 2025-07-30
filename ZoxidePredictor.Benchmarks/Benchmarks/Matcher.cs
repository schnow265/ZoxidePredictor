using System.Collections.Concurrent;
using System.Diagnostics;

using BenchmarkDotNet.Attributes;

using ZoxidePredictor.Benchmarks.Matcher;

namespace ZoxidePredictor.Benchmarks.Benchmarks;

[RPlotExporter]
public class Matcher
{
    private readonly ConcurrentDictionary<string, double> _database  = new();
    private readonly string _query = "repo";
    
    [GlobalSetup]
    public void Setup()
    {
        BuildDatabase();
    }

    [Benchmark]
    public List<string> MatcherV0() => new MatchV0(_database).Match(_query);

    [Benchmark]
    public List<string> MatcherV1() => new MatchV1(_database).Match(_query);
    
    private void BuildDatabase()
    {
        if (!_database.IsEmpty) _database.Clear();

        using var process = new Process();
        process.StartInfo.FileName = "zoxide";
        process.StartInfo.Arguments = "query --list --all --score";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = false;
        if (!process.Start())
        {
            return;
        }

        while (!process.StandardOutput.EndOfStream)
        {
            string? line = process.StandardOutput.ReadLine();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2 || !double.TryParse(parts[0], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double number))
            {
                continue;
            }

            string path = string.Join(' ', parts.Skip(1));
            _database.TryAdd(path, number);
        }
    }
}