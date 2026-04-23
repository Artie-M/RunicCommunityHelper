using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RunicCommunityHelper.Common.Entities;

[Table("Users")]
public class UserEntity
{
    public int Id { get; init; }
    
    public ulong DiscordId { get; init; }
    
    [MaxLength(128)] public required string Email { get; set; }
    
    [MaxLength(64)] public required string Salt { get; init; }
    
    [MaxLength(128)] public required string Password { get; set; }
    
    [MaxLength(64)] public string? Role { get; set; }
}