using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RunicCommunityHelper.Common.Entities;

[Table("DiscordUsers")]
public class DiscordUserEntity
{
    public int Id { get; init; }
    
    public ulong DiscordId { get; set; }
    
    [MaxLength(128)] public required string Email { get; set; }
}