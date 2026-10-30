using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Api.Security;

namespace TeaPremium.Api.Controllers;

[ApiController]
[Authorize]
[ValidateAdminAntiforgeryToken]
public abstract class AdminApiControllerBase : ControllerBase;
