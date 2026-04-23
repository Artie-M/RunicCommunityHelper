using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RunicCommunityHelper.Common.Entities;

namespace RunicCommunityHelper.Implementations.Database.Configuration;

public class UsersStoreConfiguration: IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.DiscordId).IsRequired();
        builder.Property(e => e.Email).IsRequired();
        builder.Property(e => e.Salt).IsRequired();
        builder.Property(e => e.Password).IsRequired();
    }
}