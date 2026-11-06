namespace TeaPremium.Api.Contracts.Authentication;

public sealed record CurrentAdminUserResponse(
    Guid Id,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record AntiforgeryTokenResponse(string HeaderName, string RequestToken);
