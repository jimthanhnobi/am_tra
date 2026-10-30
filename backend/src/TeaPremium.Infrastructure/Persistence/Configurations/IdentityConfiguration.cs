using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaPremium.Application.Security;
using TeaPremium.Infrastructure.Identity;

namespace TeaPremium.Infrastructure.Persistence.Configurations;

internal static class SeededIdentityIds
{
    internal static readonly Guid AdminRoleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    internal static readonly Guid EditorRoleId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    internal static readonly Guid PublisherRoleId = Guid.Parse("10000000-0000-0000-0000-000000000003");
}

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("identity_users");
        builder.HasKey(user => user.Id).HasName("pk_identity_users");

        builder.Property(user => user.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(user => user.UserName).HasColumnName("user_name").HasMaxLength(256);
        builder.Property(user => user.NormalizedUserName).HasColumnName("normalized_user_name").HasMaxLength(256);
        builder.Property(user => user.Email).HasColumnName("email").HasMaxLength(256);
        builder.Property(user => user.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(256);
        builder.Property(user => user.EmailConfirmed).HasColumnName("email_confirmed");
        builder.Property(user => user.PasswordHash).HasColumnName("password_hash");
        builder.Property(user => user.SecurityStamp).HasColumnName("security_stamp");
        builder.Property(user => user.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
        builder.Property(user => user.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(256);
        builder.Property(user => user.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
        builder.Property(user => user.TwoFactorEnabled).HasColumnName("two_factor_enabled");
        builder.Property(user => user.LockoutEnd).HasColumnName("lockout_end").HasColumnType("timestamp with time zone");
        builder.Property(user => user.LockoutEnabled).HasColumnName("lockout_enabled");
        builder.Property(user => user.AccessFailedCount).HasColumnName("access_failed_count");

        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasFilter("normalized_email IS NOT NULL")
            .HasDatabaseName("ux_identity_users_normalized_email");
        builder.HasIndex(user => user.NormalizedUserName)
            .IsUnique()
            .HasFilter("normalized_user_name IS NOT NULL")
            .HasDatabaseName("ux_identity_users_normalized_user_name");
    }
}
internal sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("identity_roles");
        builder.HasKey(role => role.Id).HasName("pk_identity_roles");

        builder.Property(role => role.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(role => role.Name).HasColumnName("name").HasMaxLength(256);
        builder.Property(role => role.NormalizedName).HasColumnName("normalized_name").HasMaxLength(256);
        builder.Property(role => role.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();

        builder.HasIndex(role => role.NormalizedName)
            .IsUnique()
            .HasFilter("normalized_name IS NOT NULL")
            .HasDatabaseName("ux_identity_roles_normalized_name");

        builder.HasData(
            new ApplicationRole
            {
                Id = SeededIdentityIds.AdminRoleId,
                Name = AdminRoles.Admin,
                NormalizedName = AdminRoles.Admin.ToUpperInvariant(),
                ConcurrencyStamp = "20000000-0000-0000-0000-000000000001",
            },
            new ApplicationRole
            {
                Id = SeededIdentityIds.EditorRoleId,
                Name = AdminRoles.Editor,
                NormalizedName = AdminRoles.Editor.ToUpperInvariant(),
                ConcurrencyStamp = "20000000-0000-0000-0000-000000000002",
            },
            new ApplicationRole
            {
                Id = SeededIdentityIds.PublisherRoleId,
                Name = AdminRoles.Publisher,
                NormalizedName = AdminRoles.Publisher.ToUpperInvariant(),
                ConcurrencyStamp = "20000000-0000-0000-0000-000000000003",
            });
    }
}

internal sealed class IdentityRoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("identity_role_claims");
        builder.HasKey(claim => claim.Id).HasName("pk_identity_role_claims");

        builder.Property(claim => claim.Id).HasColumnName("id");
        builder.Property(claim => claim.RoleId).HasColumnName("role_id");
        builder.Property(claim => claim.ClaimType)
            .HasColumnName("claim_type")
            .HasMaxLength(128);
        builder.Property(claim => claim.ClaimValue)
            .HasColumnName("claim_value")
            .HasMaxLength(256);

        builder.HasIndex(claim => new { claim.RoleId, claim.ClaimType, claim.ClaimValue })
            .IsUnique()
            .HasDatabaseName("ux_identity_role_claims_role_type_value");

        builder.HasOne<ApplicationRole>()
            .WithMany()
            .HasForeignKey(claim => claim.RoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_identity_role_claims_identity_roles_role_id");

        builder.HasData(BuildSeedClaims());
    }

    private static IEnumerable<IdentityRoleClaim<Guid>> BuildSeedClaims()
    {
        var claimId = 1;

        foreach (var permission in Permissions.All)
        {
            yield return CreateClaim(claimId++, SeededIdentityIds.AdminRoleId, permission);
        }

        foreach (var permission in new[]
                 {
                     Permissions.CatalogRead,
                     Permissions.CatalogWrite,
                     Permissions.ContentRead,
                     Permissions.ContentWrite,
                     Permissions.SeoManage,
                 })
        {
            yield return CreateClaim(claimId++, SeededIdentityIds.EditorRoleId, permission);
        }

        foreach (var permission in new[]
                 {
                     Permissions.CatalogRead,
                     Permissions.CatalogWrite,
                     Permissions.CatalogPublish,
                     Permissions.ContentRead,
                     Permissions.ContentWrite,
                     Permissions.ContentPublish,
                     Permissions.SeoManage,
                 })
        {
            yield return CreateClaim(claimId++, SeededIdentityIds.PublisherRoleId, permission);
        }
    }

    private static IdentityRoleClaim<Guid> CreateClaim(int id, Guid roleId, string permission) =>
        new()
        {
            Id = id,
            RoleId = roleId,
            ClaimType = CustomClaimTypes.Permission,
            ClaimValue = permission,
        };
}

internal sealed class IdentityUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("identity_user_claims");
        builder.HasKey(claim => claim.Id).HasName("pk_identity_user_claims");
        builder.Property(claim => claim.Id).HasColumnName("id");
        builder.Property(claim => claim.UserId).HasColumnName("user_id");
        builder.Property(claim => claim.ClaimType)
            .HasColumnName("claim_type")
            .HasMaxLength(128);
        builder.Property(claim => claim.ClaimValue)
            .HasColumnName("claim_value")
            .HasMaxLength(2048);
        builder.HasIndex(claim => claim.UserId).HasDatabaseName("ix_identity_user_claims_user_id");
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(claim => claim.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_identity_user_claims_identity_users_user_id");
    }
}

internal sealed class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("identity_user_logins");
        builder.HasKey(login => new { login.LoginProvider, login.ProviderKey })
            .HasName("pk_identity_user_logins");
        builder.Property(login => login.LoginProvider).HasColumnName("login_provider").HasMaxLength(128);
        builder.Property(login => login.ProviderKey).HasColumnName("provider_key").HasMaxLength(128);
        builder.Property(login => login.ProviderDisplayName).HasColumnName("provider_display_name");
        builder.Property(login => login.UserId).HasColumnName("user_id");
        builder.HasIndex(login => login.UserId).HasDatabaseName("ix_identity_user_logins_user_id");
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(login => login.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_identity_user_logins_identity_users_user_id");
    }
}

internal sealed class IdentityUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("identity_user_roles");
        builder.HasKey(userRole => new { userRole.UserId, userRole.RoleId })
            .HasName("pk_identity_user_roles");
        builder.Property(userRole => userRole.UserId).HasColumnName("user_id");
        builder.Property(userRole => userRole.RoleId).HasColumnName("role_id");
        builder.HasIndex(userRole => userRole.RoleId).HasDatabaseName("ix_identity_user_roles_role_id");
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_identity_user_roles_identity_users_user_id");
        builder.HasOne<ApplicationRole>()
            .WithMany()
            .HasForeignKey(userRole => userRole.RoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_identity_user_roles_identity_roles_role_id");
    }
}

internal sealed class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("identity_user_tokens");
        builder.HasKey(token => new { token.UserId, token.LoginProvider, token.Name })
            .HasName("pk_identity_user_tokens");
        builder.Property(token => token.UserId).HasColumnName("user_id");
        builder.Property(token => token.LoginProvider).HasColumnName("login_provider").HasMaxLength(128);
        builder.Property(token => token.Name).HasColumnName("name").HasMaxLength(128);
        builder.Property(token => token.Value).HasColumnName("value");
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_identity_user_tokens_identity_users_user_id");
    }
}
