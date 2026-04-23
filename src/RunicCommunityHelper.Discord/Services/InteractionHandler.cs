using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RunicCommunityHelper.Common.Const;
using RunicCommunityHelper.Common.Services;
using RunicCommunityHelper.Discord.Commands;

namespace RunicCommunityHelper.Discord.Services;

public class InteractionHandler(
    DiscordSocketClient client,
    ILogger<DiscordClientService> logger,
    InteractionService handler,
    IServiceProvider provider,
    IConfiguration configuration,
    IExceptionHandler exceptionHandler,
    IInteractionLoader loader)
    : DiscordClientService(client, logger)
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        loader.AddAssemblies(typeof(GeneralCommands).Assembly);

        Client.InteractionCreated += HandleInteraction;
        Client.GuildMemberUpdated += async (before, after) =>
        {
            await GeneralCommands.HandleRoleRemovedAsync(before, after, provider);
        };
        
        handler.InteractionExecuted += HandleInteractionExecute;

        await loader.RegisterCommandsToGuildAsync(configuration.GetValue<ulong>(DiscordConst.DevGuild));
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(Client, interaction);

            var result = await handler.ExecuteCommandAsync(context, provider);

            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                            .GetExceptionEmbedBuilder("Недосточно прав на выполнение команды")
                            .Build());
                        break;
                    case InteractionCommandError.Exception:
                        await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                            .GetExceptionEmbedBuilder("Произошла внутреняя ошибка")
                            .Build());
                        break;
                    case InteractionCommandError.UnknownCommand:
                        await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                            .GetExceptionEmbedBuilder("Неизвестная команда")
                            .Build());
                        break;
                    case InteractionCommandError.ConvertFailed:
                        await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                            .GetExceptionEmbedBuilder("Не удалось преобразовать параметр команды")
                            .Build());
                        break;
                    case InteractionCommandError.BadArgs:
                        await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                            .GetExceptionEmbedBuilder("Неправильно введены данные")
                            .Build());
                        break;
                    case InteractionCommandError.Unsuccessful:
                        await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                            .GetExceptionEmbedBuilder("Команда не смогла выполниться")
                            .Build());
                        break;
                    case InteractionCommandError.ParseFailed:
                        await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                            .GetExceptionEmbedBuilder("Не удалось преобразовать данные аргументов команды")
                            .Build());
                        break;
                    case null:
                        await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                            .GetExceptionEmbedBuilder("Неизвестная ошибка")
                            .Build());
                        break;
                    default:
                        await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                            .GetExceptionEmbedBuilder("Неизвестная ошибка")
                            .Build());
                        break;
                }
        }
        catch
        {
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(async msg => await msg.Result.DeleteAsync());
        }
    }

    private async Task HandleInteractionExecute(ICommandInfo commandInfo, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                        .GetExceptionEmbedBuilder("Недосточно прав на выполнение команды")
                        .Build());
                    break;
                case InteractionCommandError.Exception:
                    await exceptionHandler.SendDiscordErrorAsync(commandInfo, context, result);
                    break;
                case InteractionCommandError.UnknownCommand:
                    if (commandInfo.Name.StartsWith("$")) break;
                    await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                        .GetExceptionEmbedBuilder("Неизвестная команда")
                        .Build());
                    break;
                case InteractionCommandError.ConvertFailed:
                    await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                        .GetExceptionEmbedBuilder("Не удалось преобразовать параметр команды")
                        .Build());
                    break;
                case InteractionCommandError.BadArgs:
                    await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                        .GetExceptionEmbedBuilder("Неправильно введены данные")
                        .Build());
                    break;
                case InteractionCommandError.Unsuccessful:
                    await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                        .GetExceptionEmbedBuilder("Команда не смогла выполниться")
                        .Build());
                    break;
                case InteractionCommandError.ParseFailed:
                    await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                        .GetExceptionEmbedBuilder("Не удалось преобразовать данные аргументов команды")
                        .Build());
                    break;
                case null:
                    await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                        .GetExceptionEmbedBuilder("Неизвестная ошибка")
                        .Build());
                    break;
                default:
                    await exceptionHandler.RespondToUserAsync(context, exceptionHandler
                        .GetExceptionEmbedBuilder("Неизвестная ошибка")
                        .Build());
                    break;
            }
    }
}