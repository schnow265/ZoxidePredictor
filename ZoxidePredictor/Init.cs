using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Subsystem;

using ZoxidePredictor.Lib;

namespace ZoxidePredictor;

public class Init : IModuleAssemblyInitializer, IModuleAssemblyCleanup
{
    private const string Identifier = "ffdc2a29-0644-4342-b776-ceda9a057fcd";
    private ConcurrentDictionary<string, double> _database;
    private readonly Timer _timer;

    public Init()
    {
        _database = new ConcurrentDictionary<string, double>();
        Database database = new();
        _timer = new Timer(_ => Database.BuildDatabase(ref _database), null, TimeSpan.Zero, TimeSpan.FromSeconds(120));
    }

    /// <summary>
    /// Gets called when assembly is loaded.
    /// </summary>
    public void OnImport()
    {
        ZoxidePredictor zoxidePredictor = new(Identifier, ref _database);
        SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, zoxidePredictor);
    }

    /// <summary>
    /// Gets called when the binary module is unloaded.
    /// </summary>
    public void OnRemove(PSModuleInfo psModuleInfo)
    {
        SubsystemManager.UnregisterSubsystem(SubsystemKind.CommandPredictor, new Guid(Identifier));
        _timer.Dispose();
        _database.Clear();
    }
}