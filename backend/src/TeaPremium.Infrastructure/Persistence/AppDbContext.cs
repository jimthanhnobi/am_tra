using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Commerce;
using TeaPremium.Domain.Content;
using TeaPremium.Domain.Leads;
using TeaPremium.Domain.Media;
using TeaPremium.Domain.Seo;
using TeaPremium.Domain.Settings;
using TeaPremium.Infrastructure.Identity;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using TeaPremium.Application.Security;

namespace TeaPremium.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options,
    IAuditContext? auditContext = null, TimeProvider? timeProvider = null)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public const string ConnectionStringName = "Postgres";
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Only these fields retain values. Contact details, free text and Identity secrets never enter the log.
    private static readonly HashSet<string> AuditValueFields =
    ["Status", "IsSellable", "Quantity", "UnitPrice", "UnitCost", "TotalAmount", "CustomerId",
     "ProductId", "SalesOrderId", "LeadId", "Origin", "AcquisitionSource", "HadPriorPurchase",
     "ClosingChannel", "OrderedAtUtc", "CompletedAtUtc", "ReturnedAtUtc", "PublishedAt", "RoleId", "UserId"];

    private List<AuditLog> PrepareAudit()
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries<AuditLog>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit records are append-only.");
        var logs = new List<AuditLog>();
        foreach (var entry in ChangeTracker.Entries().ToArray())
        {
            if (entry.Entity is not (Customer or CustomerInteraction or SalesOrder or SalesOrderItem or
                Lead or Product or ApplicationUser or ApplicationRole or IdentityUserRole<Guid> or IdentityRoleClaim<Guid>)) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;
            var changes = new Dictionary<string, object?[]>();
            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsPrimaryKey() || property.Metadata.IsConcurrencyToken ||
                    property.Metadata.Name is "CreatedAtUtc" or "UpdatedAtUtc") continue;
                if (entry.State == EntityState.Modified && !property.IsModified) continue;
                var retain = AuditValueFields.Contains(property.Metadata.Name);
                changes[property.Metadata.Name] =
                [entry.State == EntityState.Added ? null : retain ? property.OriginalValue : "[redacted]",
                 entry.State == EntityState.Deleted ? null : retain ? property.CurrentValue : "[redacted]"];
            }
            if (changes.Count == 0) continue;
            var entityId = entry.Properties.FirstOrDefault(x => x.Metadata.Name == "Id")?.CurrentValue;
            entityId = entityId is Guid ? entityId : entry.Properties.FirstOrDefault(x => x.Metadata.Name is "UserId" or "RoleId")?.CurrentValue;
            logs.Add(new AuditLog
            {
                OccurredAtUtc = (timeProvider ?? TimeProvider.System).GetUtcNow(),
                ActorUserId = auditContext?.ActorUserId,
                CorrelationId = auditContext?.CorrelationId is { } correlation ? correlation[..Math.Min(64, correlation.Length)] : null,
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = entityId is Guid id ? id : Guid.Empty,
                Action = entry.State == EntityState.Added ? "create" : entry.State == EntityState.Deleted ? "delete" : "update",
                ChangesJson = JsonSerializer.Serialize(changes),
            });
        }
        AuditLogs.AddRange(logs);
        return logs;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var logs = PrepareAudit();
        try { return base.SaveChanges(acceptAllChangesOnSuccess); }
        catch
        {
            foreach (var log in logs) Entry(log).State = EntityState.Detached;
            throw;
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var logs = PrepareAudit();
        try { return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }
        catch
        {
            foreach (var log in logs) Entry(log).State = EntityState.Detached;
            throw;
        }
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerInteraction> CustomerInteractions => Set<CustomerInteraction>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    public DbSet<ProductMedia> ProductMedia => Set<ProductMedia>();

    public DbSet<MediaUploadSession> MediaUploadSessions => Set<MediaUploadSession>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleCategory> ArticleCategories => Set<ArticleCategory>();
    public DbSet<ArticleCategoryLink> ArticleCategoryLinks => Set<ArticleCategoryLink>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<ContactEvent> ContactEvents => Set<ContactEvent>();
    public DbSet<SeoRedirect> SeoRedirects => Set<SeoRedirect>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
