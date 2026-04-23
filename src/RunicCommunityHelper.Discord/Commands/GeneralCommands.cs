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

    [ComponentInteraction(DiscordConst.LinkEmailModal)]
    private async Task ExecuteLinkEmailModalAsync(EmailLinkModal modal)
    {
        await DeferAsync(ephemeral: true);
        
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
        await EnsureDiscordUserExist(db, Context.User.Id, modal.Email);
        var driveManager = scope.ServiceProvider.GetRequiredService<IGoogleDriveManager>();
        await UpdateDocsAccess(db, driveManager, (Context.User as IGuildUser)!, modal.Email);
        
        await FollowupAsync("Доступ обновлён!", ephemeral: true);
    }

    public static async Task HandleRoleRemovedAsync(Cacheable<SocketGuildUser, ulong> cachedBefore, SocketGuildUser after, IServiceProvider provider)
    {
        var before = await cachedBefore.GetOrDownloadAsync();
        if (before == null) return;

        var currentRoleIds = after.Roles.Select(r => r.Id).ToList();
        var removedRoles = before.Roles.Select(r => r.Id).Except(currentRoleIds).ToList();
        if (!removedRoles.Any()) return;

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
        var driveManager = scope.ServiceProvider.GetRequiredService<IGoogleDriveManager>();

        var discordUser = await db.DiscordUsers.FirstOrDefaultAsync(x => x.DiscordId == after.Id);
        if (discordUser == null) return;

        var documentsToCheck = await db.GoogleDocuments
            .Where(doc => removedRoles.Contains(doc.RequiredRole))
            .ToListAsync();

        if (!documentsToCheck.Any()) return;
        
        var filesToRevoke = new List<string>();

        foreach (var doc in documentsToCheck)
        {
            var hasAlternativeAccessRole = await db.GoogleDocuments
                .AnyAsync(d => d.FileId == doc.FileId && currentRoleIds.Contains(d.RequiredRole));

            if (!hasAlternativeAccessRole)
            {
                filesToRevoke.Add(doc.FileId);
            }
        }

        if (filesToRevoke.Any())
        {
            await driveManager.RemoveUserAccessAsync(discordUser.Email, filesToRevoke.ToArray());
        }
    }
    
    private async Task<DiscordUserEntity> EnsureDiscordUserExist(ICoreContext db, ulong discordId, string email)
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
            user.Email = email;
            await db.SaveChangesAsync();
        }

        return user;
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