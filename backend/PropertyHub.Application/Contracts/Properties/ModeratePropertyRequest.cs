using System.ComponentModel.DataAnnotations;
using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Contracts.Properties;

public sealed record ModeratePropertyRequest(
    ModerationStatus Status,
    [StringLength(500)] string? Reason);
