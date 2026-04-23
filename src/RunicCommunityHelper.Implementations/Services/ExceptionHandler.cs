using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunicCommunityHelper.Common.Const;
using RunicCommunityHelper.Common.Database;
using RunicCommunityHelper.Common.Entities;
using RunicCommunityHelper.Common.Exceptions;
using RunicCommunityHelper.Common.Services;
using Serilog;

namespace RunicCommunityHelper.Implementations.Services;

public class ExceptionHandler : IExceptionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _configuration;
    private readonly ICoreContext _store;
    private readonly IServiceScopeFactory _factory;
    private readonly ILogger<ExceptionHandler> _logger;

    public ExceptionHandler(ILogger<ExceptionHandler> logger,
        DiscordSocketClient client, IConfiguration configuration, IServiceScopeFactory factory)
    {
        _store = factory.CreateScope().ServiceProvider.GetRequiredService<ICoreContext>();
        _logger = logger;
        _client = client;
        _configuration = configuration;
        _factory = factory;
    }

    public EmbedBuilder GetExceptionEmbedBuilder(string message)
    {
        return new EmbedBuilder()
            .WithTitle("Произошла ошибка")
            .WithCurrentTimestamp()
            .WithDescription($"```\n{message}\n```")
            .WithColor(Color.Red);
    }

    public async Task SendDiscordErrorAsync(ICommandInfo commandInfo, IInteractionContext context, IResult result)
    {
        try
        {
            if (result is not ExecuteResult executeResult)
            {
                await HandleUnknownErrorAsync(context);
                return;
            }

            var isUserException = (executeResult.Exception as InteractionException)?.InnerException is UserException;
            var exceptionEntity = CreateExceptionEntity(commandInfo, context, executeResult);
            await HandleExceptionAsync(exceptionEntity, context, isUserException);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling another error.");
        }
    }

    public Task SendInternalErrorAsync(string payload, string stackTrace)
    {
        throw new NotImplementedException();
    }

    public async Task RespondToUserAsync(IInteractionContext context, Embed embed)
    {
        try
        {
            if (context.Interaction.HasResponded)
                await context.Interaction.FollowupAsync(embed: embed, ephemeral: true);
            else
                await context.Interaction.RespondAsync(embed: embed, ephemeral: true);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Can't respond to user!");
        }
    }

    private async Task HandleUnknownErrorAsync(IInteractionContext context)
    {
        var unknownError = new ExceptionEntity
        {
            Id = Guid.NewGuid(),
            Message = "Неизвестная ошибка",
            Timestamp = context.Interaction.CreatedAt
        };

        await SendExceptionEmbedAsync(unknownError, context, true);
    }

    private ExceptionEntity CreateExceptionEntity(ICommandInfo commandInfo, IInteractionContext context,
        ExecuteResult executeResult)
    {
        var commandDisplayName = GetCommandDisplayName(commandInfo);
        var payload = GetCommandParameters(commandInfo, context)
            .Select(x => $"{x.Key}: {x.Value}");


        if (executeResult.Exception is InteractionException interactionException)
            return new ExceptionEntity
            {
                Id = Guid.NewGuid(),
                ChannelId = context.Channel.Id,
                Context = "Interaction",
                IssuerId = context.User.Id,
                Message = interactionException.InnerException?.Message,
                StackTrace = interactionException.InnerException?.ToString(),
                Timestamp = context.Interaction.CreatedAt,
                Payload = $"{commandDisplayName} {string.Join(", ", payload)}"
            };

        return new ExceptionEntity
        {
            Id = Guid.NewGuid(),
            ChannelId = context.Channel.Id,
            Context = "ApplicationCommand",
            IssuerId = context.User.Id,
            Message = executeResult.ErrorReason,
            StackTrace = executeResult.Exception.ToString(),
            Timestamp = context.Interaction.CreatedAt,
            Payload = $"{commandDisplayName} {string.Join(", ", payload)}"
        };
    }

    private string GetCommandDisplayName(ICommandInfo commandInfo)
    {
        return commandInfo switch
        {
            SlashCommandInfo slashCommandInfo => slashCommandInfo.Module.IsSlashGroup &&
                                                 !slashCommandInfo.IgnoreGroupNames
                ? slashCommandInfo.ToString()
                : slashCommandInfo.Name,
            _ => commandInfo.Name
        };
    }

    private async Task HandleExceptionAsync(ExceptionEntity exceptionEntity, IInteractionContext context,
        bool isUserException)
    {
        if (!isUserException)
        {
            _store.Exceptions.Add(exceptionEntity);
            await _store.SaveChangesAsync();
        }

        await SendExceptionEmbedAsync(exceptionEntity, context, isUserException);
    }

    private async Task SendExceptionEmbedAsync(ExceptionEntity exceptionEntity, IInteractionContext context,
        bool isUserException = false)
    {
        if (!isUserException) await SendAdminEmbedAsync(exceptionEntity, context);

        await ReportExceptionToUserAsync(exceptionEntity, context, isUserException);
    }

    private async Task SendAdminEmbedAsync(ExceptionEntity exceptionEntity, IInteractionContext context)
    {
        var embedWithExceptionForAdmin = new EmbedBuilder()
            .WithTitle("EID: " + exceptionEntity.Id)
            .WithDescription(
                $"User: {context.User.Mention} `@{context.User.Username}` ({context.User.Id})\n" +
                $"Date: {TimestampTag.FromDateTimeOffset(exceptionEntity.Timestamp)}\n" +
                $"Command: `{exceptionEntity.Payload ?? "???"}`\n" +
                $"Channel: `#{context.Channel.Name}`\n" +
                $"StackTrace: ```\n{exceptionEntity.StackTrace ?? "???"}```")
            .WithColor(Color.Red);

        var testGuild = _client.GetGuild(_configuration.GetValue<ulong>(DiscordConst.DevGuild));
        var errorChannel = testGuild.GetTextChannel(_configuration.GetValue<ulong>(DiscordConst.ExceptionChannel));

        await errorChannel.SendMessageAsync(embed: embedWithExceptionForAdmin.Build());
    }

    private async Task ReportExceptionToUserAsync(ExceptionEntity exceptionEntity, IInteractionContext context,
        bool isUserException)
    {
        var message = !isUserException
            ? "Произошла внутренняя ошибка, сообщите код ошибки администратору"
            : exceptionEntity.Message ?? "Неизвестная ошибка";

        var embedBuilder = GetExceptionEmbedBuilder(message);

        if (!isUserException) embedBuilder.WithFooter($"EID: {exceptionEntity.Id}");

        await RespondToUserAsync(context, embedBuilder.Build());
    }

    private Dictionary<string, string> GetCommandParameters(ICommandInfo commandInfo, IInteractionContext context)
    {
        return context.Interaction switch
        {
            ISlashCommandInteraction slashCommandInteraction => HandleSlashData((SlashCommandInfo)commandInfo,
                slashCommandInteraction),
            IComponentInteraction componentInteraction => HandleComponentData((ComponentCommandInfo)commandInfo,
                componentInteraction),
            _ => new Dictionary<string, string>()
        };
    }

    private Dictionary<string, string> HandleSlashData(SlashCommandInfo info, ISlashCommandInteraction interaction)
    {
        var parameters = info.Parameters.Select(x => x.Name).ToList();
        var interactionParameters = info.IsTopLevelCommand
            ? interaction.Data.Options
            : interaction.Data.Options.First().Options;

        var dictionary = new Dictionary<string, string>();

        foreach (var parameter in interactionParameters)
        {
            var data = parameters.FirstOrDefault(x => x == parameter.Name);
            if (data is not null) dictionary.Add(parameter.Name, parameter.Value?.ToString() ?? "null");
        }

        return dictionary;
    }

    private Dictionary<string, string> HandleComponentData(ComponentCommandInfo info, IComponentInteraction interaction)
    {
        return new Dictionary<string, string> { { "ID", interaction.Data.CustomId } };
    }
}