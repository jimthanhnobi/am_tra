using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Authentication;
using TeaPremium.Application.Security;

namespace TeaPremium.Api.Controllers;

[Route("admin/users"), Authorize(Policy = Permissions.UsersManage)]
public sealed class AdminUsersController(IAdminUserService users) : AdminApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AdminUserDto>> Create(
        CreateAdminUserCommand command, CancellationToken cancellationToken)
    {
        var user = await users.CreateAsync(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user);
    }
}
