using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RunicCommunityHelper.Common.Entities;

namespace RunicCommunityHelper.Implementations.Database.Configuration;

public class GoogleDocumentsStoreConfiguration : IEntityTypeConfiguration<GoogleDocumentEntity>
{
    public void Configure(EntityTypeBuilder<GoogleDocumentEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.FileId).IsRequired();
        builder.Property(e => e.RequiredRole).IsRequired();
    }
}