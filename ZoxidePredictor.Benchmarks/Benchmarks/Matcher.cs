using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management.Automation.Subsystem.Prediction;

using BenchmarkDotNet.Attributes;

using ZoxidePredictor.Lib.Matcher;

namespace ZoxidePredictor.Benchmarks.Benchmarks;

[RPlotExporter]
public class Matcher
{
    private ConcurrentDictionary<string, double> _database = new();
    private readonly string _query = "repo";

    [GlobalSetup]
    public void Setup()
    {
        BuildDatabase();
    }

    [Benchmark]
    public List<PredictiveSuggestion> MatcherV0()
    {
        return new MatchV0().Match(_query, ref _database);
    }

    [Benchmark]
    public List<PredictiveSuggestion> MatcherV1()
    {
        return new MatchV1().Match(_query, ref _database);
    }
    
    [Benchmark]
    public List<PredictiveSuggestion> MatcherV2() => new MatchV2().Match(_query, ref _database);

    private void BuildDatabase()
    {
        if (!_database.IsEmpty)
        {
            _database.Clear();
        }

        using Process process = new();
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