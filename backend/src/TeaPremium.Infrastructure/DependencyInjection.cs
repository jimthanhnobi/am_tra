using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeaPremium.Application.Authentication;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Media;
using TeaPremium.Infrastructure.Catalog;
using TeaPremium.Infrastructure.Identity;
using TeaPremium.Infrastructure.Media;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(AppDbContext.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{AppDbContext.ConnectionStringName}' is required. " +
                "Configure it with user secrets or the " +
                $"ConnectionStrings__{AppDbContext.ConnectionStringName} environment variable.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(
                    typeof(AppDbContext).Assembly.GetName().Name)));

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version2;

                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAdminBootstrapper, DevelopmentAdminBootstrapper>();
        services.AddScoped<IAdminCatalogSummaryQuery, AdminCatalogSummaryQuery>();
        services.AddScoped<ICatalogDataStore, CatalogDataStore>();
        services.AddScoped<IMediaDataStore, MediaDataStore>();

        ConfigureObjectStorage(services, configuration);

        return services;
    }

    private static void ConfigureObjectStorage(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var defaults = MediaUploadSettings.Default;
        var maximumFileSize = configuration.GetValue<long?>(
            "MediaStorage:MaximumFileSizeBytes") ?? defaults.MaximumFileSizeBytes;
        var uploadLifetimeMinutes = configuration.GetValue<int?>(
            "MediaStorage:UploadUrlLifetimeMinutes") ?? (int)defaults.UploadUrlLifetime.TotalMinutes;
        var inspectionPrefixBytes = configuration.GetValue<int?>(
            "MediaStorage:InspectionPrefixBytes") ?? defaults.InspectionPrefixBytes;

        if (maximumFileSize <= 0 || uploadLifetimeMinutes is < 1 or > 60 || inspectionPrefixBytes < 32)
        {
            throw new InvalidOperationException("Media storage limits are invalid.");
        }

        services.AddSingleton(new MediaUploadSettings(
            maximumFileSize,
            TimeSpan.FromMinutes(uploadLifetimeMinutes),
            inspectionPrefixBytes));

        var accountId = configuration["R2_ACCOUNT_ID"]?.Trim();
        var bucketName = configuration["R2_BUCKET_NAME"]?.Trim();
        var accessKeyId = configuration["R2_ACCESS_KEY_ID"]?.Trim();
        var secretAccessKey = configuration["R2_SECRET_ACCESS_KEY"]?.Trim();
        var publicBaseUrl = configuration["R2_PUBLIC_BASE_URL"]?.Trim();
        string?[] values = [accountId, bucketName, accessKeyId, secretAccessKey, publicBaseUrl];

        if (values.All(string.IsNullOrWhiteSpace))
        {
            services.AddSingleton<IObjectStorageService, UnavailableObjectStorageService>();
            return;
        }

        if (values.Any(string.IsNullOrWhiteSpace) ||
            !Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var publicUri) ||
            publicUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException(
                "R2_ACCOUNT_ID, R2_BUCKET_NAME, R2_ACCESS_KEY_ID, R2_SECRET_ACCESS_KEY, and " +
                "a valid R2_PUBLIC_BASE_URL must be configured together.");
        }

        var options = new CloudflareR2Options(
            accountId!,
            bucketName!,
            accessKeyId!,
            secretAccessKey!,
            publicUri.ToString().TrimEnd('/'));
        services.AddSingleton(options);
        services.AddSingleton<IAmazonS3>(_ =>
        {
            var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
            var clientConfiguration = new AmazonS3Config
            {
                ServiceURL = $"https://{options.AccountId}.r2.cloudflarestorage.com",
                AuthenticationRegion = "auto",
                ForcePathStyle = true,
            };
            return new AmazonS3Client(credentials, clientConfiguration);
        });
        services.AddSingleton<IObjectStorageService, CloudflareR2StorageService>();
    }
}

