using FluentAssertions;
using Moq;
using PropertyHub.Application.Contracts.Cities;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Application.Models.Cities;
using PropertyHub.Application.Services;
using PropertyHub.Domain.Entities;

namespace PropertyHub.UnitTests.Services;

public sealed class CityServiceTests
{
    private readonly Mock<ICityRepository> _repository = new();

    [Fact]
    public async Task ListActiveAsync_ShouldMapRepositoryCities()
    {
        _repository.Setup(repository => repository.ListActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new City
                {
                    Id = Guid.NewGuid(),
                    Name = "Lahore",
                    IsActive = true,
                    Latitude = 31.520400m,
                    Longitude = 74.358700m
                }
            ]);
        var service = new CityService(_repository.Object);

        var response = await service.ListActiveAsync(CancellationToken.None);

        response.Items.Should().ContainSingle()
            .Which.Name.Should().Be("Lahore");
    }

    [Fact]
    public async Task CreateAsync_ShouldTrimNormalizeAndPersistCity()
    {
        City? capturedCity = null;
        _repository.Setup(repository => repository.NameExistsAsync(
                "SIALKOT",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(repository => repository.AddAsync(
                It.IsAny<City>(),
                It.IsAny<CancellationToken>()))
            .Callback<City, CancellationToken>((city, _) => capturedCity = city)
            .Returns(Task.CompletedTask);
        var service = new CityService(_repository.Object);

        var result = await service.CreateAsync(
            new CreateCityRequest("  Sialkot  ", 32.494500m, 74.522900m),
            CancellationToken.None);

        result.Outcome.Should().Be(CityMutationOutcome.Success);
        result.City!.Name.Should().Be("Sialkot");
        capturedCity!.NormalizedName.Should().Be("SIALKOT");
        _repository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectDuplicateNormalizedName()
    {
        _repository.Setup(repository => repository.NameExistsAsync(
                "LAHORE",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new CityService(_repository.Object);

        var result = await service.CreateAsync(
            new CreateCityRequest(" lahore ", 31.520400m, 74.358700m),
            CancellationToken.None);

        result.Outcome.Should().Be(CityMutationOutcome.DuplicateName);
        _repository.Verify(
            repository => repository.AddAsync(It.IsAny<City>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectNameThatIsBlankAfterTrimming()
    {
        var service = new CityService(_repository.Object);

        var result = await service.CreateAsync(
            new CreateCityRequest("  ", 31.520400m, 74.358700m),
            CancellationToken.None);

        result.Outcome.Should().Be(CityMutationOutcome.InvalidName);
        _repository.Verify(
            repository => repository.NameExistsAsync(
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFoundWhenCityDoesNotExist()
    {
        _repository.Setup(repository => repository.GetForUpdateAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((City?)null);
        var service = new CityService(_repository.Object);

        var result = await service.UpdateAsync(
            Guid.NewGuid(),
            new UpdateCityRequest("Sialkot", 32.494500m, 74.522900m, true),
            CancellationToken.None);

        result.Outcome.Should().Be(CityMutationOutcome.NotFound);
        _repository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnInUseWithoutDeletingReferencedCity()
    {
        var city = new City { Id = Guid.NewGuid(), Name = "Lahore" };
        _repository.Setup(repository => repository.GetForUpdateAsync(
                city.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(city);
        _repository.Setup(repository => repository.IsReferencedAsync(
                city.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new CityService(_repository.Object);

        var outcome = await service.DeleteAsync(city.Id, CancellationToken.None);

        outcome.Should().Be(CityMutationOutcome.InUse);
        _repository.Verify(repository => repository.Remove(It.IsAny<City>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUnusedCity()
    {
        var city = new City { Id = Guid.NewGuid(), Name = "Sialkot" };
        _repository.Setup(repository => repository.GetForUpdateAsync(
                city.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(city);
        _repository.Setup(repository => repository.IsReferencedAsync(
                city.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = new CityService(_repository.Object);

        var outcome = await service.DeleteAsync(city.Id, CancellationToken.None);

        outcome.Should().Be(CityMutationOutcome.Success);
        _repository.Verify(repository => repository.Remove(city), Times.Once);
        _repository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
