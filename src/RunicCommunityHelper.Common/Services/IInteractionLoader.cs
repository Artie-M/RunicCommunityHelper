using System.Reflection;

namespace RunicCommunityHelper.Common.Services;

public interface IInteractionLoader
{
    void AddAssembly(Assembly assembly);
    void AddAssemblies(IEnumerable<Assembly> assemblies);
    void AddAssemblies(params Assembly[] assemblies);

    Task RegisterCommandsToGuildAsync(ulong guildId);
    Task RegisterCommandsGloballyAsync();

    Task UnregisterCommandsFromGuildAsync(ulong guildId);
    Task UnregisterCommandsGloballyAsync();
}