using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Subsystem;
using System.Management.Automation.Subsystem.Prediction;

namespace snowUtils.Binary.Subsystems
{
    public class ZoxidePredictor : ICommandPredictor, IDisposable
    {
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
        public string Description => "Predictor & Feedback Provider for zoxide";

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

            // Special case: "cd " with no argument, suggest most used directory
            if (input.Length == 3 && input[2] == ' ')
            {
                // O(n) but only one pass, faster than full sort for a single best
                KeyValuePair<string, double>? best = null;
                foreach (var kv in _database)
                {
                    if (best == null || kv.Value > best.Value.Value)
                        best = kv;
                }
                return best is not null
                    ? new SuggestionPackage([new PredictiveSuggestion("cd " + best.Value.Key)])
                    : default;
            }

            // Handle "cd <path>"
            if (input.Length <= 3 || !input.StartsWith("cd ", StringComparison.Ordinal))
            {
                return default;
            }


            var path = input.Substring(3).Trim();

            // Replace '\' or '/' with spaces in the path for the contains check
            string pathForContains = path.Replace('\\', ' ').Replace('/', ' ');

            // If path is empty after trimming, return top 10 by score
            if (string.IsNullOrEmpty(path))
            {
                // Partial selection: avoid full sort if _database is large
                var top = _database.Count <= 10
                    ? _database.Select(kv => new PredictiveSuggestion("cd " + kv.Key)).ToList()
                    : _database.OrderByDescending(kv => kv.Value)
                        .Take(10)
                        .Select(kv => new PredictiveSuggestion("cd " + kv.Key))
                        .ToList();
                return new SuggestionPackage(top);
            }

            // Suggest directories starting with the path (case-insensitive), top 10 by score
            var startsWithFiltered = _database
                .Where(kv => kv.Key.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .Select(kv => new PredictiveSuggestion("cd " + kv.Key))
                .ToList();

            // If we have enough, return
            if (startsWithFiltered.Count > 0)
                return new SuggestionPackage(startsWithFiltered);

            // Also return directories that contain the (normalized) path, top 10 by score
            var containsFiltered = _database
                .Where(kv =>
                {
                    // Normalize both the key and search string
                    string normalizedKey = kv.Key.Replace('\\', ' ').Replace('/', ' ');
                    return normalizedKey.IndexOf(pathForContains, StringComparison.OrdinalIgnoreCase) >= 0;
                })
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .Select(kv => new PredictiveSuggestion("cd " + kv.Key))
                .ToList();

            return containsFiltered.Count > 0 ? new SuggestionPackage(containsFiltered) : default;
        }

        public void BuildDatabase()
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

        public void Dispose()
        {
            _database.Clear();
            _timer.Dispose();
        }

        #region "interface methods for processing feedback"

        public bool CanAcceptFeedback(PredictionClient client, PredictorFeedbackKind feedback) => true;
        public void OnSuggestionDisplayed(PredictionClient client, uint session, int countOrIndex) { }
        public void OnSuggestionAccepted(PredictionClient client, uint session, string acceptedSuggestion) { }
        public void OnCommandLineAccepted(PredictionClient client, IReadOnlyList<string> history) { }
        public void OnCommandLineExecuted(PredictionClient client, string commandLine, bool success) { }

        #endregion;
    }

    public class Init : IModuleAssemblyInitializer, IModuleAssemblyCleanup
    {
        private const string Identifier = "ffdc2a29-0644-4342-b776-ceda9a057fcd";

        /// <summary>
        /// Gets called when assembly is loaded.
        /// </summary>
        public void OnImport()
        {
            var zoxidePredictor = new ZoxidePredictor(Identifier);
            SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, zoxidePredictor);
        }

        /// <summary>
        /// Gets called when the binary module is unloaded.
        /// </summary>
        public void OnRemove(PSModuleInfo psModuleInfo)
        {
            SubsystemManager.UnregisterSubsystem(SubsystemKind.CommandPredictor, new Guid(Identifier));
        }
    }
}