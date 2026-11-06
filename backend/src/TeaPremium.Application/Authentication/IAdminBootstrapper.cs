namespace TeaPremium.Application.Authentication;

public interface IAdminBootstrapper
{
    Task<AdminBootstrapResult> BootstrapAsync(CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(CancellationToken cancellationToken = default);
}

public sealed record AdminBootstrapResult(bool UserCreated, bool RoleGranted);
