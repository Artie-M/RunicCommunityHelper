using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RunicCommunityHelper.Common.Entities;

namespace RunicCommunityHelper.Implementations.Database.Configuration;

public class ExceptionStoreConfiguration : IEntityTypeConfiguration<ExceptionEntity>
{
    public void Configure(EntityTypeBuilder<ExceptionEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Timestamp);
        builder.Property(x => x.IssuerId).IsRequired(false);
        builder.Property(x => x.ChannelId).IsRequired(false);
        builder.Property(x => x.Message).IsRequired(false);
        builder.Property(x => x.StackTrace).IsRequired(false);
        builder.Property(x => x.Context).IsRequired(false);
        builder.Property(x => x.Payload).IsRequired(false);
    }
}