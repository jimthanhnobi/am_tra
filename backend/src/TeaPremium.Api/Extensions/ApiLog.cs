namespace TeaPremium.Api.Extensions;

public static partial class ApiLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Starting {ApiName} with contract version {ApiVersion}")]
    public static partial void StartingApi(
        ILogger logger,
        string apiName,
        string apiVersion);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Development admin bootstrap completed. UserCreated={UserCreated}, RoleGranted={RoleGranted}")]
    public static partial void AdminBootstrapCompleted(
        ILogger logger,
        bool userCreated,
        bool roleGranted);
}

