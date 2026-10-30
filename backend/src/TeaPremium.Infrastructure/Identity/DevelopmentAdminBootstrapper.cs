using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using TeaPremium.Application.Authentication;
using TeaPremium.Application.Security;

namespace TeaPremium.Infrastructure.Identity;

internal sealed class DevelopmentAdminBootstrapper(
    IConfiguration configuration,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : IAdminBootstrapper
{
    private const string EmailConfigurationKey = "AuthBootstrap:Email";
    private const string PasswordConfigurationKey = "AuthBootstrap:Password";

    public async Task<AdminBootstrapResult> BootstrapAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = configuration[EmailConfigurationKey]?.Trim();
        var password = configuration[PasswordConfigurationKey];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Set AuthBootstrap__Email and AuthBootstrap__Password through environment " +
                "variables or the API user-secrets store before running bootstrap-admin.");
        }

        if (!await roleManager.RoleExistsAsync(AdminRoles.Admin))
        {
            throw new InvalidOperationException(
                "The Admin role does not exist. Apply the reviewed Identity migration first.");
        }

        var user = await userManager.FindByEmailAsync(email);
        var userCreated = false;

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                LockoutEnabled = true,
            };

            var createResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(createResult, "create the development admin user");
            userCreated = true;
        }

        var roleGranted = false;
        if (!await userManager.IsInRoleAsync(user, AdminRoles.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(user, AdminRoles.Admin);
            EnsureSucceeded(roleResult, "grant the Admin role");
            roleGranted = true;
        }

        return new AdminBootstrapResult(userCreated, roleGranted);
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errorCodes = string.Join(", ", result.Errors.Select(error => error.Code));
        throw new InvalidOperationException($"Unable to {operation}. Identity errors: {errorCodes}.");
    }
}
