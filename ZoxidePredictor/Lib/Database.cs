using System.Collections.Concurrent;
using System.Diagnostics;

namespace ZoxidePredictor.Lib;

public class Database
{
    public static void BuildDatabase(ref ConcurrentDictionary<string, double> database)
    {
        if (!database.IsEmpty)
        {
            database.Clear();
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
            database.TryAdd(path, number);
        }
    }
}