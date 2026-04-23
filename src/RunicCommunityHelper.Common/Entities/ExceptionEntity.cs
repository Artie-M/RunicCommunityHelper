using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RunicCommunityHelper.Common.Entities;

[Table("Exceptions")]
public class ExceptionEntity
{
    [Key] public Guid Id { get; set; }

    public ulong? IssuerId { get; set; }

    public ulong? ChannelId { get; set; }

    public string? Context { get; set; }

    public string? Message { get; set; }

    public string? StackTrace { get; set; }

    public string? Payload { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}