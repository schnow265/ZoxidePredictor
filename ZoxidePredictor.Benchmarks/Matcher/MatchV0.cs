using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ZoxidePredictor.Benchmarks.Matcher;

public class MatchV0(ConcurrentDictionary<string, double> database)
{
    public List<string> Match(string query)
    {
        // Split query into terms
        var terms = Regex.Split(query.Trim(), @"\s+")
                         .Where(t => !string.IsNullOrEmpty(t))
                         .ToArray();

        if (terms.Length == 0)
            return new List<string>();

        // Get the last component of the last term (for rule 3)
        string lastTerm = terms.Last();
        string lastComponent = lastTerm.Contains('/')
            ? lastTerm.Substring(lastTerm.LastIndexOf('/') + 1)
            : lastTerm;

        // Build sequence of terms to match in order (case-insensitive)
        var lowerTerms = terms.Select(t => t.ToLowerInvariant()).ToArray();

        // Create list of (path, frecency) to sort by frecency descending
        var matches = new List<(string path, double frecency)>();

        foreach (var kvp in database)
        {
            string path = kvp.Key;
            double frecency = kvp.Value;

            string lowerPath = path.ToLowerInvariant();

            // 1. Case-insensitive
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
            string[] pathComponents = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (pathComponents.Length == 0)
                continue;

            string pathLastComponent = pathComponents.Last();
            if (!pathLastComponent.Equals(lastComponent, StringComparison.OrdinalIgnoreCase))
                continue;

            // Passed all checks, add to matches
            matches.Add((path, frecency));
        }

        // 4. Return in descending order of frecency
        return matches
            .OrderByDescending(m => m.frecency)
            .Select(m => m.path)
            .ToList();
    }
}