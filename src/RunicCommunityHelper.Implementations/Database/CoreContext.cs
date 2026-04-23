using Microsoft.EntityFrameworkCore;
using RunicCommunityHelper.Common.Database;
using RunicCommunityHelper.Common.Entities;
using RunicCommunityHelper.Implementations.Database.Configuration;

namespace RunicCommunityHelper.Implementations.Database;

public class CoreContext : DbContext, ICoreContext
{
    public CoreContext(DbContextOptions<CoreContext> options) : base(options)
    {
    }
    
    public DbSet<ExceptionEntity> Exceptions => Set<ExceptionEntity>();
    
    public DbSet<UserEntity> Users => Set<UserEntity>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UsersStoreConfiguration());
        modelBuilder.ApplyConfiguration(new ExceptionStoreConfiguration());
        
        base.OnModelCreating(modelBuilder);
    }
}