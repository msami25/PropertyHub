using System.ComponentModel.DataAnnotations;

namespace PropertyHub.Application.Contracts.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(100)] string Password);
