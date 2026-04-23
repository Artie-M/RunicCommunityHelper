using Discord.Interactions;
using Discord.Interactions.Builders;
using Serilog;

namespace RunicCommunityHelper.Common.Discord;

public abstract class DiscordModuleBase : InteractionModuleBase<SocketInteractionContext>
{
    public override void Construct(ModuleBuilder builder, InteractionService commandService)
    {
        Log.Logger.Information("Constructing module {Module} with {count} slash-commands", builder.Name,
            builder.SlashCommands.Count);
        base.Construct(builder, commandService);
    }
}