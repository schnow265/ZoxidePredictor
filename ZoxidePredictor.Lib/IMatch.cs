using System.Collections.Concurrent;
using System.Management.Automation.Subsystem.Prediction;

namespace ZoxidePredictor.Lib;

public interface IMatch
{
    public List<PredictiveSuggestion> Match(string query, ref ConcurrentDictionary<string, double> database);
}