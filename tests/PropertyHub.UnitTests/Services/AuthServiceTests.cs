using FluentAssertions;
using Moq;
using PropertyHub.Application.Contracts.Auth;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Auth;
using PropertyHub.Application.Services;
using PropertyHub.Domain.Authorization;
using PropertyHub.Domain.Enums;

namespace PropertyHub.UnitTests.Services;

public sealed class AuthServiceTests
{
    private readonly Mock<IUserAccountRepository> _repository = new();
    private readonly Mock<IJwtTokenService> _tokenService = new();

    [Fact]
    public async Task LoginAsync_ShouldIssueTokenForValidActiveAccount()
    {
        var account = CreateAccount();
        var expected = new AuthTokenResponse(
            "token",
            "Bearer",
            DateTime.UtcNow.AddMinutes(30),
            new AuthUserResponse(account.Id, account.FullName, account.Email, RoleNames.User));
        _repository.Setup(repository => repository.ValidateCredentialsAsync(
                account.Email,
                "StrongPass!123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CredentialValidationResult.Success(account));
        _tokenService.Setup(service => service.CreateToken(account)).Returns(expected);
        var service = new AuthService(_repository.Object, _tokenService.Object);

        var result = await service.LoginAsync(
            new LoginRequest(account.Email, "StrongPass!123"),
            CancellationToken.None);

        result.Outcome.Should().Be(LoginOutcome.Success);
        result.Response.Should().Be(expected);
        _tokenService.Verify(service => service.CreateToken(account), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldNotIssueTokenForDisabledAccount()
    {
        _repository.Setup(repository => repository.ValidateCredentialsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CredentialValidationResult.Disabled());
        var service = new AuthService(_repository.Object, _tokenService.Object);

        var result = await service.LoginAsync(
            new LoginRequest("disabled@propertyhub.test", "StrongPass!123"),
            CancellationToken.None);

        result.Outcome.Should().Be(LoginOutcome.Disabled);
        result.Response.Should().BeNull();
        _tokenService.Verify(service => service.CreateToken(It.IsAny<AccountSnapshot>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldTrimInputAndReturnUserRole()
    {
        var account = CreateAccount();
        _repository.Setup(repository => repository.CreateUserAsync(
                account.FullName,
                account.Email,
                "StrongPass!123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccountCreationResult.Success(account));
        var service = new AuthService(_repository.Object, _tokenService.Object);

        var result = await service.RegisterAsync(
            new RegisterRequest($" {account.FullName} ", $" {account.Email} ", "StrongPass!123"),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Response!.Role.Should().Be(RoleNames.User);
        _repository.VerifyAll();
    }

    private static AccountSnapshot CreateAccount() =>
        new(
            Guid.NewGuid(),
            "Test User",
            "user@propertyhub.test",
            AccountStatus.Active,
            0,
            [RoleNames.User]);
}
