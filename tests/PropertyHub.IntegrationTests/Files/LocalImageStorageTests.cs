using FluentAssertions;
using PropertyHub.Infrastructure.Files;

namespace PropertyHub.IntegrationTests.Files;

public sealed class LocalImageStorageTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        $"PropertyHubStorage-{Guid.NewGuid():N}");

    [Fact]
    public async Task Storage_ShouldSaveReadAndDeleteOnlyWithinConfiguredRoot()
    {
        var storage = new LocalImageStorage(_rootPath);
        byte[] content = [1, 2, 3];

        await storage.SaveAsync("property/image.png", content, CancellationToken.None);
        await using (var stream = await storage.OpenReadAsync(
            "property/image.png",
            CancellationToken.None))
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            buffer.ToArray().Should().Equal(content);
        }

        await storage.DeleteAsync("property/image.png", CancellationToken.None);
        File.Exists(Path.Combine(_rootPath, "property", "image.png")).Should().BeFalse();
    }

    [Theory]
    [InlineData("../escape.png")]
    [InlineData(@"..\escape.png")]
    [InlineData(@"C:\escape.png")]
    public async Task Storage_ShouldRejectTraversalAndRootedPaths(string path)
    {
        var storage = new LocalImageStorage(_rootPath);

        var action = () => storage.SaveAsync(path, new byte[] { 1 }, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
