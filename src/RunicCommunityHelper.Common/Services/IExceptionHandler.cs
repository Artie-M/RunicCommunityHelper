using Discord;
using Discord.Interactions;

namespace RunicCommunityHelper.Common.Services;

public interface IExceptionHandler
{
    EmbedBuilder GetExceptionEmbedBuilder(string message);

    /// <summary>
    ///     Sends a Discord error message for a given command and interaction context.
    /// </summary>
    /// <param name="commandInfo">The command information.</param>
    /// <param name="context">The interaction context.</param>
    /// <param name="result">The result of the command execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendDiscordErrorAsync(ICommandInfo commandInfo, IInteractionContext context, IResult result);

    Task SendInternalErrorAsync(string payload, string stackTrace);

    Task RespondToUserAsync(IInteractionContext context, Embed embed);
}