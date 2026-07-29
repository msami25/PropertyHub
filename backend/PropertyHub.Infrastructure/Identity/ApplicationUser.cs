using Microsoft.AspNetCore.Identity;
using PropertyHub.Domain.Enums;

namespace PropertyHub.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public int TokenVersion { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
