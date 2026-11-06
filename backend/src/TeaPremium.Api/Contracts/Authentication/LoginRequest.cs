using System.ComponentModel.DataAnnotations;

namespace TeaPremium.Api.Contracts.Authentication;

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(256, MinimumLength = 1)] string Password,
    bool RememberMe = false);
