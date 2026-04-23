using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RunicCommunityHelper.Common.Const;
using RunicCommunityHelper.Common.Database;
using RunicCommunityHelper.Common.Discord;
using RunicCommunityHelper.Discord.Modals;

namespace RunicCommunityHelper.Discord.Commands;

public class GeneralCommands : DiscordModuleBase
{
    private readonly IServiceProvider _provider;

    public GeneralCommands(IServiceProvider provider)
    {
        _provider = provider;
    }

    [DefaultPermission(false)]
    [SlashCommand("create-email-link-form", "Отправить сообщение с компонентом для заполнения формы регистрации")]
    public async Task CreateEmailLinkFormAsync([Summary("channel", "Канал отправки сообщения")] IChannel channel)
    {
        if (channel is not ITextChannel socketChannel)
        {
            await RespondAsync("Неподходящий тип канала.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        var componentBuilder = new ComponentBuilderV2([
            new ContainerBuilder()
                .WithAccentColor(DiscordConst.DefaultColor)
                .WithTextDisplay($"""
                                  # Доступ к документам
                                  Нажмите на кнопку, чтобы получить доступ к документам и таблицам.
                                  """)
                .WithActionRow(new ActionRowBuilder()
                    .WithButton("Обновить доступ",
                        DiscordConst.LinkEmailButton,
                        ButtonStyle.Primary,
                        new Emoji("📬")))
        ]);

        await socketChannel.SendMessageAsync(components: componentBuilder.Build());
        await FollowupAsync("Форма создана.", ephemeral: true);
    }

    [ComponentInteraction(DiscordConst.LinkEmailButton)]
    private async Task ExecuteLinkEmailButtonAsync()
    {
        await RespondWithModalAsync<EmailLinkModal>(DiscordConst.LinkEmailModal);
    }

    [ComponentInteraction(DiscordConst.LinkEmailModal)]
    private async Task ExecuteLinkEmailModalAsync(EmailLinkModal modal)
    {
        // проверить, имеет ли почта, указанная в modal.Email доступ к Google-Таблице
        // если не имеет, выдать доступ
    }

    private async Task<bool> CheckAccess()
    {
        
        return false;
    }
}