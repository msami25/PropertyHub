namespace PropertyHub.Application.Interfaces.Services;

public interface IImageStorage
{
    Task SaveAsync(
        string relativePath,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken);
}
