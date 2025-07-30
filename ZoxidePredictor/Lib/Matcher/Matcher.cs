using System.Collections.Concurrent;
using System.Management.Automation.Subsystem.Prediction;

namespace ZoxidePredictor.Lib.Matcher;

/// <summary>
/// C# reimplementation of the Zoxide matching algorithm
/// from a <see cref="ConcurrentDictionary{TKey,TValue}"/> with a string key (path) and double value (score)
/// </summary>
public static class Matcher
{
    /// <summary>
    /// Return predictions following the algorithm from zoxide
    /// </summary>
    /// <param name="query">The folder query. Same you would just pass to zoxide to cd.</param>
    /// <param name="database">A Reference to the built database</param>
    /// <returns></returns>
    public static List<PredictiveSuggestion> Match(string query, ref ConcurrentDictionary<string, double> database)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        // Normalize query
        var terms = SplitTerms(query);
        if (terms.Count == 0)
            return [];

        // Last term split for "last component" logic
        var lastTerm = terms.Last();
        var lastTermComponents = lastTerm.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        var lastKeyword = lastTermComponents.LastOrDefault() ?? lastTerm;

        var matches = new List<(string Path, double Score)>();

        foreach ((string path, double frecency) in database)
        {
            if (IsMatch(path, terms, lastKeyword))
            {
                matches.Add((path,frecency));
            }
        }

        // Sort by descending frecency, then by path (for stable ordering)
        return matches
            .OrderByDescending(m => m.Score)
            .ThenBy(m => m.Path, StringComparer.OrdinalIgnoreCase)
            .Select(m => new PredictiveSuggestion("cd " + m.Path))
            .ToList();
    }

    // Split query into terms, preserving slashes/backslashes as separate terms
    private static List<string> SplitTerms(string query)
    {
        var terms = new List<string>();
        int i = 0, n = query.Length;
        while (i < n)
        {
            if (query[i] == '/' || query[i] == '\\')
            {
                terms.Add(query[i].ToString());
                i++;
                continue;
            }

            int start = i;
            while (i < n && query[i] != '/' && query[i] != '\\')
                i++;

            var term = query.Substring(start, i - start).Trim();
            if (!string.IsNullOrEmpty(term))
                terms.Add(term);
        }
        return terms;
    }

    // Main matching logic with partial last-component match support
    private static bool IsMatch(string path, List<string> terms, string lastKeyword)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // Normalize path separators: treat both '\' and '/' as equivalent
        string pathNorm = path.Replace('\\', '/');
        string pathLower = pathNorm.ToLowerInvariant();

        // For extracting components, split on both / and \
        var pathComponents = path
            .ToLowerInvariant()
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

        int pos = 0;
        foreach (var term in terms)
        {
            if (term is "/" or "\\")
            {
                // Next term must start after a slash or backslash
                int slashPos = pathLower.IndexOf('/', pos);
                int backslashPos = pathLower.IndexOf('\\', pos);
                int nextSep = -1;
                if (slashPos == -1) nextSep = backslashPos;
                else if (backslashPos == -1) nextSep = slashPos;
                else nextSep = Math.Min(slashPos, backslashPos);

                if (nextSep == -1)
                    return false;
                pos = nextSep + 1;
                continue;
            }
            // Find the next occurrence of the term in path, after pos
            int foundPos = pathLower.IndexOf(term.ToLowerInvariant(), pos, StringComparison.Ordinal);
            if (foundPos == -1)
                return false;
            pos = foundPos + term.Length;
        }

        // The last component of the last keyword must match (fully or partially) the last component of the path
        if (pathComponents.Length == 0)
            return false;
        var lastComponent = pathComponents.Last();

        // Accept full match or partial (contains) match
        return lastComponent.Contains(lastKeyword.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }
}