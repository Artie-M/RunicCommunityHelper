using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RunicCommunityHelper.Common.Const;
using RunicCommunityHelper.Common.Utils;
using RunicCommunityHelper.Discord.Services;

namespace RunicCommunityHelper.Discord;

public static class DependencyInjection
{
    /// <summary>
    ///     Adds Discord services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration object.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddDiscord(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDiscordHost((config, provider) =>
        {
            config.SocketConfig = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 1000,
                GatewayIntents = GatewayIntents.All
            };

            config.Token = EnvironmentSecrets.Get(DiscordConst.Token) ??
                           configuration.GetValue<string>("Discord:Token")!;
        });

        services.AddInteractionService((config, _) =>
        {
            config.LogLevel = LogSeverity.Info;
            config.UseCompiledLambda = true;
            config.DefaultRunMode = RunMode.Async;
        });

        services.AddSingleton(new InteractiveConfig { DefaultTimeout = TimeSpan.FromMinutes(5) });
        services.AddSingleton<InteractiveService>();

        services.AddHostedService<InteractionHandler>();

        return services;
    }
}