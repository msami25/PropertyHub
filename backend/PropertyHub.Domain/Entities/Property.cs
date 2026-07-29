using PropertyHub.Domain.Enums;

namespace PropertyHub.Domain.Entities;

public sealed class Property
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SellerProfileId { get; set; }
    public Guid CityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string NormalizedTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PropertyPurpose Purpose { get; set; }
    public PropertyType PropertyType { get; set; }
    public string Address { get; set; } = string.Empty;
    public string NormalizedAddress { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Area { get; set; }
    public AreaUnit AreaUnit { get; set; } = AreaUnit.SquareFeet;
    public decimal AreaSquareFeet { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public string ContactNumber { get; set; } = string.Empty;
    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;
    public AvailabilityStatus AvailabilityStatus { get; set; } = AvailabilityStatus.Available;
    public string? RejectionReason { get; set; }
    public Guid? ModeratedByUserId { get; set; }
    public DateTime? ModeratedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }
    public SellerProfile SellerProfile { get; set; } = null!;
    public City City { get; set; } = null!;
    public ICollection<PropertyImage> Images { get; set; } = [];
}
