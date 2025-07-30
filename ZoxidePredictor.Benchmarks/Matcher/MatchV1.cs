using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ZoxidePredictor.Benchmarks.Matcher;

public partial class MatchV1(ConcurrentDictionary<string, double> database)
{
    [GeneratedRegex(@"\s+", RegexOptions.IgnoreCase)]
    private partial Regex TermSplitter();

    public List<string> Match(string query)
    {
        var terms = TermSplitter()
            .Split(query.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToArray();

        if (terms.Length == 0)
            return [];

        // Get the last component of the last term (for rule 3)
        string lastTerm = terms.Last();
        string lastComponent = lastTerm.Contains('/')
            ? lastTerm[(lastTerm.LastIndexOf('/') + 1)..]
            : lastTerm;

        // Build sequence of terms to match in order (case-insensitive)
        var lowerTerms = terms.Select(t => t.ToLowerInvariant()).ToArray();

        // Create list of (path, frecency) to sort by frecency descending
        var matches = new List<(string path, double frecency)>();

        foreach ((string path, double frecency) in database)
        {
            // 1. Case-insensitive
            string lowerPath = path.ToLowerInvariant();

            // 2. All terms (including slashes) must be present in order
            int idx = 0;
            bool allTermsMatch = true;
            foreach (string term in lowerTerms)
            {
                idx = lowerPath.IndexOf(term, idx, StringComparison.Ordinal);
                if (idx == -1)
                {
                    allTermsMatch = false;
                    break;
                }

                idx += term.Length;
            }

            if (!allTermsMatch)
                continue;

            // 3. Last component of last keyword must match last component of the path
            string[] pathComponents = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
            if (pathComponents.Length == 0)
                continue;

            string pathLastComponent = pathComponents.Last();
            if (!pathLastComponent.Equals(lastComponent, StringComparison.OrdinalIgnoreCase))
                continue;

            // Passed all checks, add to matches
            matches.Add((path: path, frecency: frecency));
        }

        // 4. Return in descending order of frecency
        return matches
            .OrderByDescending(m => m.frecency)
            .Select(m => m.path)
            .ToList();
    }
}