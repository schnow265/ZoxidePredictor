using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management.Automation.Subsystem.Prediction;
using System.Text.RegularExpressions;

namespace ZoxidePredictor
{
    public partial class ZoxidePredictor : ICommandPredictor, IDisposable
    {
        [GeneratedRegex(@"\s+", RegexOptions.IgnoreCase, "en-US")]
        private partial Regex TermSplitter();

        private readonly Timer _timer;
        private readonly ConcurrentDictionary<string, double> _database;

        internal ZoxidePredictor(string guid)
        {
            _database = new ConcurrentDictionary<string, double>();
            Id = new Guid(guid);

            _timer = new Timer(_ => BuildDatabase(), null, TimeSpan.Zero, TimeSpan.FromSeconds(120));
        }

        /// <summary>
        /// Gets the unique identifier for a subsystem implementation.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the name of a subsystem implementation.
        /// </summary>
        public string Name => "zoxide";

        /// <summary>
        /// Gets the description of a subsystem implementation.
        /// </summary>
        public string Description => "PSReadline Predictor for zoxide";

        /// <summary>
        /// Get the predictive suggestions. It indicates the start of a suggestion rendering session.
        /// </summary>
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="context">The <see cref="PredictionContext"/> object to be used for prediction.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the prediction.</param>
        /// <returns>An instance of <see cref="SuggestionPackage"/>.</returns>
        public SuggestionPackage GetSuggestion(PredictionClient client, PredictionContext context, CancellationToken cancellationToken)
        {
            string input = context.InputAst.Extent.Text;
            if (string.IsNullOrWhiteSpace(input) || !input.StartsWith("cd", StringComparison.Ordinal))
                return default;

            // Handle "cd <path>"
            if (input.Length <= 3 || !input.StartsWith("cd ", StringComparison.Ordinal))
            {
                return default;
            }


            var path = input.Substring(3).Trim();

            List<PredictiveSuggestion> matches = Match(path);
            
            return matches.Count > 0 ? new SuggestionPackage(matches) : default;
        }

        private void BuildDatabase()
        {
            if (_database.Count != 0) _database.Clear();

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

        private List<PredictiveSuggestion> Match(string query)
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

            foreach ((string path, double frecency) in _database)
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
                .Select(m => new PredictiveSuggestion("cd " + m.path))
                .ToList();
        }
        
        public void Dispose()
        {
            _timer.Dispose();
            _database.Clear();
        }

        #region "interface methods for processing feedback"

        public bool CanAcceptFeedback(PredictionClient client, PredictorFeedbackKind feedback) => true;
        public void OnSuggestionDisplayed(PredictionClient client, uint session, int countOrIndex) { }
        public void OnSuggestionAccepted(PredictionClient client, uint session, string acceptedSuggestion) { }
        public void OnCommandLineAccepted(PredictionClient client, IReadOnlyList<string> history) { }
        public void OnCommandLineExecuted(PredictionClient client, string commandLine, bool success) { }

        #endregion;
    }
}