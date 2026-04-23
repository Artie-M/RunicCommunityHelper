using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RunicCommunityHelper.Common.Entities;

namespace RunicCommunityHelper.Implementations.Database.Configuration;

public class StaffUsersStoreConfiguration : IEntityTypeConfiguration<StaffUserEntity>
{
    public void Configure(EntityTypeBuilder<StaffUserEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Username).IsRequired();
        builder.Property(e => e.Email);
        builder.Property(e => e.Password).IsRequired();
        builder.Property(e => e.Salt).IsRequired();
        builder.Property(e => e.Role).IsRequired();
    }
}