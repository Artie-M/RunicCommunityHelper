using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RunicCommunityHelper.Common.Entities;

[Table("GoogleDocuments")]
public class GoogleDocumentEntity
{
    public int Id { get; init; }
    
    [MaxLength(128)] public required string FileId { get; set; }
    
    [MaxLength(64)] public ulong RequiredRole { get; set; }
}