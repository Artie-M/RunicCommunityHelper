using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RunicCommunityHelper.Common.Entities;

namespace RunicCommunityHelper.Implementations.Database.Configuration;

public class DiscordUsersStoreConfiguration : IEntityTypeConfiguration<DiscordUserEntity>
{
    public void Configure(EntityTypeBuilder<DiscordUserEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.DiscordId).IsRequired();
        builder.Property(e => e.Email).IsRequired();
    }
}