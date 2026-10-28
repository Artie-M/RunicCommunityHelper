using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunicCommunityHelper.Common.Const;
using RunicCommunityHelper.Common.Database;
using RunicCommunityHelper.Common.Services;

namespace RunicCommunityHelper.Discord.Services;

public class RoleAccessService(
    DiscordSocketClient client,
    ILogger<DiscordClientService> logger,
    IServiceScopeFactory scopeFactory,
    IGoogleDriveManager driveManager,
    IConfiguration configuration)
    : DiscordClientService(client, logger), IRoleAccessService
{
    private ulong GuildId => configuration.GetValue<ulong>(DiscordConst.DevGuild);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Client.GuildMemberUpdated += HandleGuildMemberUpdatedAsync;
        Client.UserLeft += HandleUserLeftAsync;

        await Client.WaitForReadyAsync(cancellationToken);
        await SynchronizeAllAsync(cancellationToken);
    }

    public async Task RevokeObsoleteAccessAsync(ulong discordId, IReadOnlyCollection<ulong> roleIds,
        IReadOnlyCollection<ulong>? removedRoleIds = null)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();

        var discordUser = await db.DiscordUsers.FirstOrDefaultAsync(x => x.DiscordId == discordId);
        if (discordUser == null) return;

        var documents = await db.GoogleDocuments.ToListAsync();
        var allowedFiles = documents
            .Where(doc => roleIds.Contains(doc.RequiredRole))
            .Select(doc => doc.FileId)
            .ToHashSet();

        var candidates = removedRoleIds == null
            ? documents
            : documents.Where(doc => removedRoleIds.Contains(doc.RequiredRole));

        var filesToRevoke = candidates
            .Select(doc => doc.FileId)
            .Distinct()
            .Where(fileId => !allowedFiles.Contains(fileId))
            .ToArray();

        if (filesToRevoke.Length == 0) return;

        Logger.LogInformation("Revoking access to {Count} document(s) from Discord user {DiscordId}",
            filesToRevoke.Length, discordId);
        await driveManager.RemoveUserAccessAsync(discordUser.Email, filesToRevoke);
    }

    public async Task SynchronizeAllAsync(CancellationToken cancellationToken = default)
    {
        List<ulong> discordIds;
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
            discordIds = await db.DiscordUsers
                .Select(x => x.DiscordId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        Logger.LogInformation("Checking document access of {Count} user(s)", discordIds.Count);

        foreach (var discordId in discordIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var roleIds = await GetCurrentRoleIdsAsync(discordId);
                await RevokeObsoleteAccessAsync(discordId, roleIds);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogError(ex, "Failed to check document access of Discord user {DiscordId}", discordId);
            }
        }

        Logger.LogInformation("Document access check completed");
    }

    private async Task<IReadOnlyCollection<ulong>> GetCurrentRoleIdsAsync(ulong discordId)
    {
        IGuildUser? member = Client.GetGuild(GuildId)?.GetUser(discordId);
        member ??= await Client.Rest.GetGuildUserAsync(GuildId, discordId);

        return member?.RoleIds ?? [];
    }

    private Task HandleGuildMemberUpdatedAsync(Cacheable<SocketGuildUser, ulong> cachedBefore, SocketGuildUser after)
    {
        if (after.Guild.Id != GuildId) return Task.CompletedTask;

        var roleIds = after.Roles.Select(r => r.Id).ToList();
        List<ulong>? removedRoleIds = null;

        if (cachedBefore.HasValue)
        {
            removedRoleIds = cachedBefore.Value.Roles.Select(r => r.Id).Except(roleIds).ToList();
            if (removedRoleIds.Count == 0) return Task.CompletedTask;
        }

        _ = RunSafeAsync(after.Id, () => RevokeObsoleteAccessAsync(after.Id, roleIds, removedRoleIds));
        return Task.CompletedTask;
    }

    private Task HandleUserLeftAsync(SocketGuild guild, SocketUser user)
    {
        if (guild.Id != GuildId) return Task.CompletedTask;

        _ = RunSafeAsync(user.Id, () => RevokeObsoleteAccessAsync(user.Id, []));
        return Task.CompletedTask;
    }

    private async Task RunSafeAsync(ulong discordId, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to revoke document access of Discord user {DiscordId}", discordId);
        }
    }
}
