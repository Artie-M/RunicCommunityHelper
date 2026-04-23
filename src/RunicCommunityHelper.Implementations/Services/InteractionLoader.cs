using System.Reflection;
using Discord.Addons.Hosting.Util;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using RunicCommunityHelper.Common.Services;
using RunicCommunityHelper.Common.Discord;

namespace RunicCommunityHelper.Implementations.Services;

public class InteractionLoader : IInteractionLoader
{
    private readonly DiscordSocketClient _client;
    private readonly HashSet<Assembly> _discordAssemblies = new();
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _provider;
    private readonly CancellationTokenSource _tokenSource;

    public InteractionLoader(InteractionService interactionService, IServiceScopeFactory factory,
        DiscordSocketClient client, CancellationTokenSource tokenSource)
    {
        _interactionService = interactionService;
        _client = client;
        _tokenSource = tokenSource;
        _provider = factory.CreateScope().ServiceProvider;
    }

    public void AddAssembly(Assembly assembly)
    {
        _discordAssemblies.Add(assembly);
    }

    public void AddAssemblies(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies) AddAssembly(assembly);
    }

    public void AddAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies) AddAssembly(assembly);
    }

    public async Task RegisterCommandsToGuildAsync(ulong guildId)
    {
        await RegisterModulesToInteractionServiceAsync();

        await _client.WaitForReadyAsync(_tokenSource.Token);

        await _interactionService.RegisterCommandsToGuildAsync(guildId);

        Log.Logger.Information("Commands registered to guild {Guild}", _client.GetGuild(guildId)?.Name);
    }

    public async Task RegisterCommandsGloballyAsync()
    {
        await RegisterModulesToInteractionServiceAsync();

        await _client.WaitForReadyAsync(_tokenSource.Token);

        await _interactionService.RegisterCommandsGloballyAsync();

        Log.Logger.Information("Commands registered globally");
    }

    public async Task UnregisterCommandsFromGuildAsync(ulong guildId)
    {
        var guild = _client.GetGuild(guildId);

        if (guild is null)
        {
            Log.Logger.Information("Guild not found with ID {id}", guildId);
            return;
        }

        var commands = await guild.GetApplicationCommandsAsync();

        foreach (var command in commands) await command.DeleteAsync();

        Log.Logger.Information("Commands removed from guild {Guild}", _client.GetGuild(guildId)?.Name);
    }

    public async Task UnregisterCommandsGloballyAsync()
    {
        await _client.WaitForReadyAsync(_tokenSource.Token);
        var commands = await _client.GetGlobalApplicationCommandsAsync();

        foreach (var command in commands) await command.DeleteAsync();

        Log.Logger.Information("Commands removed globally");
    }

    private async Task RegisterModulesToInteractionServiceAsync()
    {
        var types = new HashSet<Type>(_discordAssemblies
            .SelectMany(x => x.GetTypes())
            .Where(DiscordModulePredicate));

        foreach (var type in types)
        {
            var moduleInfo = await _interactionService.AddModuleAsync(type, _provider);
            Log.Logger.Information("Register module {Module}", moduleInfo.Name);
        }
    }

    private static bool DiscordModulePredicate(Type type)
    {
        return type is { IsAbstract: false, IsInterface: false, IsNested: false } 
               && typeof(DiscordModuleBase).IsAssignableFrom(type);
    }
}