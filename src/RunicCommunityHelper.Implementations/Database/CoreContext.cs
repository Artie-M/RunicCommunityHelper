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
    
    public DbSet<DiscordUserEntity> DiscordUsers => Set<DiscordUserEntity>();
    
    public DbSet<StaffUserEntity> Staff => Set<StaffUserEntity>();
    
    public DbSet<GoogleDocumentEntity> GoogleDocuments => Set<GoogleDocumentEntity>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DiscordUsersStoreConfiguration());
        modelBuilder.ApplyConfiguration(new ExceptionStoreConfiguration());
        modelBuilder.ApplyConfiguration(new GoogleDocumentsStoreConfiguration());
        modelBuilder.ApplyConfiguration(new StaffUsersStoreConfiguration());
        
        base.OnModelCreating(modelBuilder);
    }
}