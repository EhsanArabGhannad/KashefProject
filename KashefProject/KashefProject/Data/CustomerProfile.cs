using System.ComponentModel.DataAnnotations;

namespace KashefProject.Data;

public sealed class CustomerProfile
{
    public int Id { get; set; }
    [MaxLength(450)] public required string UserId { get; set; }
    [MaxLength(120)] public required string FullName { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
