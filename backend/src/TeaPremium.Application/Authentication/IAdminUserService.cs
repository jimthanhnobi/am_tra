namespace TeaPremium.Application.Authentication;

public sealed record CreateAdminUserCommand(string Email, string Password, string Role);
public sealed record AdminUserDto(Guid Id, string Email, string Role);

public interface IAdminUserService
{
    Task<AdminUserDto> CreateAsync(CreateAdminUserCommand command, CancellationToken cancellationToken);
}

public sealed class AdminUserInputException(string message) : Exception(message);
public sealed class AdminUserConflictException(string message) : Exception(message);
