using System.Collections.Concurrent;
using System.Management.Automation.Subsystem.Prediction;

using ZoxidePredictor.Lib;

namespace ZoxidePredictor;

public class ZoxidePredictor : ICommandPredictor
{
    private readonly Database _dbBuilder = new();
    private ConcurrentDictionary<string, double> _database;

    internal ZoxidePredictor(string guid, ref ConcurrentDictionary<string, double> database)
    {
        _database = database;
        Id = new Guid(guid);
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
    public SuggestionPackage GetSuggestion(PredictionClient client, PredictionContext context,
        CancellationToken cancellationToken)
    {
        string input = context.InputAst.Extent.Text;
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith("cd", StringComparison.Ordinal))
        {
            return default;
        }

        // Handle "cd <path>"
        if (input.Length <= 3 || !input.StartsWith("cd ", StringComparison.Ordinal))
        {
            return default;
        }

        if (input == "cd ")
        {
            // O(n) but only one pass, faster than full sort for a single best
            KeyValuePair<string, double>? best = null;
            foreach (KeyValuePair<string, double> kv in _database)
            {
                if (best == null || kv.Value > best.Value.Value)
                {
                    best = kv;
                }
            }

            return best is not null
                ? new SuggestionPackage([new PredictiveSuggestion("cd " + best.Value.Key)])
                : default;
        }

        string path = input[3..].Trim();

        List<PredictiveSuggestion> matches = Matcher.Match(path, ref _database);

        return matches.Count > 0 ? new SuggestionPackage(matches) : default;
    }
    
    #region "interface methods for processing feedback"

    public bool CanAcceptFeedback(PredictionClient client, PredictorFeedbackKind feedback)
    {
        return true;
    }

    public void OnSuggestionDisplayed(PredictionClient client, uint session, int countOrIndex) { }
    public void OnSuggestionAccepted(PredictionClient client, uint session, string acceptedSuggestion) { }
    public void OnCommandLineAccepted(PredictionClient client, IReadOnlyList<string> history) { }
    public void OnCommandLineExecuted(PredictionClient client, string commandLine, bool success) { }

    #endregion;
}