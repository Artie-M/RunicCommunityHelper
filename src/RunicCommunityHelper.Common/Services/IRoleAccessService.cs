namespace RunicCommunityHelper.Common.Services;

public interface IRoleAccessService
{
    Task RevokeObsoleteAccessAsync(ulong discordId, IReadOnlyCollection<ulong> roleIds,
        IReadOnlyCollection<ulong>? removedRoleIds = null);

    Task SynchronizeAllAsync(CancellationToken cancellationToken = default);
}
