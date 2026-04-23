using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RunicCommunityHelper.Common.Entities;

namespace RunicCommunityHelper.Common.Database;

public interface ICoreContext
{
    DbSet<ExceptionEntity> Exceptions { get; }
    
    DbSet<UserEntity> Users { get; }
    
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}