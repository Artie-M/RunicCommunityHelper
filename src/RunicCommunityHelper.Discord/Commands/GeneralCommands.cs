using Discord;
using Discord.Interactions;
using Discord.Interactions.Builders;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunicCommunityHelper.Common.Const;
using RunicCommunityHelper.Common.Database;
using RunicCommunityHelper.Common.Discord;
using RunicCommunityHelper.Common.Entities;
using RunicCommunityHelper.Common.Services;
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

    [ModalInteraction(DiscordConst.LinkEmailModal)]
    private async Task ExecuteLinkEmailModalAsync(EmailLinkModal modal)
    {
        await DeferAsync(ephemeral: true);
        
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
        var driveManager = scope.ServiceProvider.GetRequiredService<IGoogleDriveManager>();
        await EnsureDiscordUserExist(db, driveManager, Context.User.Id, modal.Email);
        await UpdateDocsAccess(db, driveManager, (Context.User as IGuildUser)!, modal.Email);
        
        await FollowupAsync("Доступ обновлён!", ephemeral: true);
    }

    private async Task<DiscordUserEntity> EnsureDiscordUserExist(ICoreContext db, IGoogleDriveManager driveManager,
        ulong discordId, string email)
    {
        var user = await db.DiscordUsers.FirstOrDefaultAsync(x => x.DiscordId == discordId);

        if (user == null)
        {
            user = new DiscordUserEntity
            {
                DiscordId = Context.User.Id,
                Email = email
            };
            await db.DiscordUsers.AddAsync(user);
            await db.SaveChangesAsync();
        }
        else if (user.Email != email)
        {
            var oldEmail = user.Email;
            user.Email = email;
            await db.SaveChangesAsync();

            if (!string.Equals(oldEmail, email, StringComparison.OrdinalIgnoreCase))
            {
                await RevokeOldEmailAccess(db, driveManager, oldEmail);
            }
        }

        return user;
    }

    private static async Task RevokeOldEmailAccess(ICoreContext db, IGoogleDriveManager driveManager, string oldEmail)
    {
        var isUsedByOtherUser = await db.DiscordUsers
            .AnyAsync(x => x.Email.ToLower() == oldEmail.ToLower());
        if (isUsedByOtherUser) return;

        var fileIds = await db.GoogleDocuments
            .Select(x => x.FileId)
            .Distinct()
            .ToArrayAsync();

        await driveManager.RemoveUserAccessAsync(oldEmail, fileIds);
    }

    private async Task UpdateDocsAccess(ICoreContext db, IGoogleDriveManager driveManager, IGuildUser user, string email)
    {
        var fileIds = await db.GoogleDocuments
            .Where(x => user.RoleIds.Contains(x.RequiredRole))
            .Select(x => x.FileId)
            .ToArrayAsync();

        await driveManager.AddUserAsEditorAsync(email, fileIds);
    }
}