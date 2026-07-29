namespace PropertyHub.Domain.Entities;

public sealed class PropertyImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PropertyId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public byte SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public Property Property { get; set; } = null!;
}
