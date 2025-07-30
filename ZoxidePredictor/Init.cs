using System.Management.Automation;
using System.Management.Automation.Subsystem;

namespace ZoxidePredictor;

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