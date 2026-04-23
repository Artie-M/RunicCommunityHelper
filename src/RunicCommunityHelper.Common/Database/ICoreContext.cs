using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RunicCommunityHelper.Common.Entities;

namespace RunicCommunityHelper.Common.Database;

public interface ICoreContext
{
    DbSet<ExceptionEntity> Exceptions { get; }
    
    DbSet<DiscordUserEntity> DiscordUsers { get; }
    
    DbSet<StaffUserEntity> Staff { get; }
    
    DbSet<GoogleDocumentEntity> GoogleDocuments { get; }
    
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}