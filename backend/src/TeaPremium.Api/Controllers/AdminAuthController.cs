using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using TeaPremium.Api.Contracts.Authentication;
using TeaPremium.Api.Security;
using TeaPremium.Application.Security;
using TeaPremium.Infrastructure.Identity;

namespace TeaPremium.Api.Controllers;

[Route("admin/auth")]
public sealed class AdminAuthController(
    IAntiforgery antiforgery,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : AdminApiControllerBase
{
    [HttpGet("csrf")]
    [AllowAnonymous]
    public ActionResult<AntiforgeryTokenResponse> GetAntiforgeryToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);

        Response.Headers[HeaderNames.CacheControl] = "no-store";
        Response.Headers[HeaderNames.Pragma] = "no-cache";

        return Ok(new AntiforgeryTokenResponse(
            tokens.HeaderName ?? AdminSecurityDefaults.AntiforgeryHeaderName,
            tokens.RequestToken ?? throw new InvalidOperationException(
                "The antiforgery system did not issue a request token.")));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AdminSecurityDefaults.LoginRateLimitPolicy)]
    public async Task<ActionResult<CurrentAdminUserResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = request.Email.Trim();
        var result = await signInManager.PasswordSignInAsync(
            email,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Không thể đăng nhập.",
                detail: "Email hoặc mật khẩu không hợp lệ.");
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            await signInManager.SignOutAsync();
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Không thể đăng nhập.",
                detail: "Email hoặc mật khẩu không hợp lệ.");
        }

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        return Ok(await BuildCurrentUserAsync(user, principal));
    }

    [HttpGet("me")]
    public async Task<ActionResult<CurrentAdminUserResponse>> GetCurrentUser()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(await BuildCurrentUserAsync(user, User));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    private async Task<CurrentAdminUserResponse> BuildCurrentUserAsync(
        ApplicationUser user,
        ClaimsPrincipal principal)
    {
        var roles = (await userManager.GetRolesAsync(user))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var permissions = principal.FindAll(CustomClaimTypes.Permission)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return new CurrentAdminUserResponse(
            user.Id,
            user.Email ?? user.UserName ?? string.Empty,
            roles,
            permissions);
    }
}
