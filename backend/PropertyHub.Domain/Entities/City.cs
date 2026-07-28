namespace PropertyHub.Domain.Entities;

public sealed class City
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public ICollection<Property> Properties { get; set; } = [];
}
