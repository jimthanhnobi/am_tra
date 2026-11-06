using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeaPremium.Application.Authentication;
using TeaPremium.Application.Security;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.Infrastructure.Identity;

internal sealed class AdminUserService(
    AppDbContext db,
    UserManager<ApplicationUser> users,
    RoleManager<ApplicationRole> roles) : IAdminUserService
{
    public async Task<AdminUserDto> CreateAsync(
        CreateAdminUserCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var email = command.Email?.Trim() ?? string.Empty;
        if (email.Length is < 3 or > 254 || !email.Contains('@') || email.Contains(' '))
            throw new AdminUserInputException("Email không hợp lệ.");
        if (command.Password is null || command.Password.Length > 256)
            throw new AdminUserInputException("Mật khẩu không hợp lệ.");
        if (command.Role is not (AdminRoles.Editor or AdminRoles.Publisher))
            throw new AdminUserInputException("Vai trò không hợp lệ.");
        if (!await roles.RoleExistsAsync(command.Role))
            throw new AdminUserInputException("Vai trò chưa được cấu hình.");
        if (await users.FindByEmailAsync(email) is not null)
            throw new AdminUserConflictException("Email này đã có tài khoản.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = email, Email = email,
            EmailConfirmed = true, LockoutEnabled = true,
        };
        var created = await users.CreateAsync(user, command.Password);
        if (!created.Succeeded)
            throw new AdminUserInputException(string.Join(" ", created.Errors.Select(error =>
                error.Code.StartsWith("Duplicate", StringComparison.Ordinal) ? "Email này đã có tài khoản." :
                "Mật khẩu cần ít nhất 12 ký tự, chữ hoa, chữ thường, số và ký tự đặc biệt.")));
        var roleAdded = await users.AddToRoleAsync(user, command.Role);
        if (!roleAdded.Succeeded)
            throw new AdminUserInputException("Không thể cấp vai trò cho tài khoản.");
        await transaction.CommitAsync(cancellationToken);
        return new AdminUserDto(user.Id, email, command.Role);
    }
}
