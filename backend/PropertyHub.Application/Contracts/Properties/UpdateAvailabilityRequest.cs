using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Contracts.Properties;

public sealed record UpdateAvailabilityRequest(AvailabilityStatus Status);
