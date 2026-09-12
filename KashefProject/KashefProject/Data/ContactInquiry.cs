using System.ComponentModel.DataAnnotations;

namespace KashefProject.Data;

public enum ContactInquiryStatus { New, Read, Archived }
public enum ContactInquiryNotificationStatus { Pending, Sending, Sent, Failed }

public sealed class ContactInquiry
{
    public int Id { get; set; }

    [MaxLength(32)]
    public required string Reference { get; set; }

    [MaxLength(120)]
    public required string Name { get; set; }

    [MaxLength(254)]
    public required string Email { get; set; }

    [MaxLength(80)]
    public required string Interest { get; set; }

    [MaxLength(4000)]
    public required string Message { get; set; }

    public ContactInquiryStatus Status { get; set; } = ContactInquiryStatus.New;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public ContactInquiryNotificationStatus NotificationStatus { get; set; } = ContactInquiryNotificationStatus.Pending;
    public DateTime NextAttemptUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptUtc { get; set; }
    public DateTime? SentUtc { get; set; }
    public int AttemptCount { get; set; }

    [MaxLength(120)]
    public string? ProviderMessageId { get; set; }

    [MaxLength(500)]
    public string? LastError { get; set; }
}
