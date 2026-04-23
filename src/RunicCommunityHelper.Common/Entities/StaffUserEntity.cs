using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RunicCommunityHelper.Common.Entities;

[Table("Staff")]
public class StaffUserEntity
{
    public int Id { get; init; }
    
    [MaxLength(32)] public required string Username { get; init; }
    
    [MaxLength(128)] public string? Email { get; set; }
    
    [MaxLength(64)] public required string Salt { get; init; }
    
    [MaxLength(128)] public required string Password { get; set; }
    
    [MaxLength(64)] public required string Role { get; set; }
}