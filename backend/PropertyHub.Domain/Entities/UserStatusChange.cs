using PropertyHub.Domain.Enums;

namespace PropertyHub.Domain.Entities;

public sealed class UserStatusChange
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TargetUserId { get; set; }
    public Guid AdminUserId { get; set; }
    public AccountStatus PreviousStatus { get; set; }
    public AccountStatus NewStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
