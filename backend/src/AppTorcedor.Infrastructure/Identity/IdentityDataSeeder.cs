using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppTorcedor.Infrastructure.Identity;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var env = services.GetRequiredService<IHostEnvironment>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var db = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in SystemRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
                continue;
            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName) { Id = Guid.NewGuid() })
                .ConfigureAwait(false);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to create role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await SeedPermissionsAsync(db, roleManager, cancellationToken).ConfigureAwait(false);

        if (!await db.LegalDocuments.AnyAsync(cancellationToken).ConfigureAwait(false)
            && env.IsEnvironment("Testing")
            && configuration.GetValue("Testing:SeedMinimalLegalDocuments", false))
            await SeedMinimalPublicLegalDocumentsAsync(db, cancellationToken).ConfigureAwait(false);

        // Development: garante termos + privacidade com versão publicada (cadastro público /register).
        // Corrige bancos já existentes com documentos só em rascunho ou com um único tipo cadastrado.
        if (env.IsDevelopment())
            await EnsureDevelopmentRegistrationLegalDocumentsAsync(db, cancellationToken).ConfigureAwait(false);

        var email = configuration["Seed:AdminMaster:Email"] ?? "admin@torcedor.local";
        var password =
            configuration["Seed:AdminMaster:Password"]
            ?? Environment.GetEnvironmentVariable("ADMIN_MASTER_INITIAL_PASSWORD");

        if (string.IsNullOrWhiteSpace(password))
        {
            if (env.IsDevelopment() || env.IsEnvironment("Testing"))
                password = "ChangeMe_Integration1!";
            else
                throw new InvalidOperationException(
                    "ADMIN_MASTER_INITIAL_PASSWORD or Seed:AdminMaster:Password must be set outside Development/Testing.");
        }

        var admin = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Name = "Administrador Master",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            var create = await userManager.CreateAsync(admin, password).ConfigureAwait(false);
            if (!create.Succeeded)
                throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", create.Errors.Select(e => e.Description))}");
        }

        if (!await userManager.IsInRoleAsync(admin, SystemRoles.AdministradorMaster).ConfigureAwait(false))
        {
            var addRole = await userManager.AddToRoleAsync(admin, SystemRoles.AdministradorMaster).ConfigureAwait(false);
            if (!addRole.Succeeded)
                throw new InvalidOperationException($"Failed to assign Administrador Master: {string.Join(", ", addRole.Errors.Select(e => e.Description))}");
        }

        if (configuration.GetValue("Testing:SeedSampleUsers", false))
            await SeedSampleUsersAsync(db, userManager, cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedMinimalPublicLegalDocumentsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var termsId = Guid.NewGuid();
        var privacyId = Guid.NewGuid();
        var termsVersionId = Guid.NewGuid();
        var privacyVersionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.LegalDocuments.AddRange(
            new LegalDocumentRecord
            {
                Id = termsId,
                Type = LegalDocumentType.TermsOfUse,
                Title = "Termos de uso (teste)",
                CreatedAt = now,
            },
            new LegalDocumentRecord
            {
                Id = privacyId,
                Type = LegalDocumentType.PrivacyPolicy,
                Title = "Política de privacidade (teste)",
                CreatedAt = now,
            });

        db.LegalDocumentVersions.AddRange(
            new LegalDocumentVersionRecord
            {
                Id = termsVersionId,
                LegalDocumentId = termsId,
                VersionNumber = 1,
                Content = "Conteúdo dos termos (ambiente de teste).",
                Status = LegalDocumentVersionStatus.Published,
                PublishedAt = now,
                CreatedAt = now,
            },
            new LegalDocumentVersionRecord
            {
                Id = privacyVersionId,
                LegalDocumentId = privacyId,
                VersionNumber = 1,
                Content = "Conteúdo da política (ambiente de teste).",
                Status = LegalDocumentVersionStatus.Published,
                PublishedAt = now,
                CreatedAt = now,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureDevelopmentRegistrationLegalDocumentsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        await EnsureLegalDocumentWithPublishedVersionAsync(
                db,
                LegalDocumentType.TermsOfUse,
                "Termos de uso (desenvolvimento)",
                "Texto mínimo para permitir cadastro público em desenvolvimento. Substitua pelo conteúdo oficial no backoffice LGPD.",
                cancellationToken)
            .ConfigureAwait(false);
        await EnsureLegalDocumentWithPublishedVersionAsync(
                db,
                LegalDocumentType.PrivacyPolicy,
                "Política de privacidade (desenvolvimento)",
                "Texto mínimo para permitir cadastro público em desenvolvimento. Substitua pelo conteúdo oficial no backoffice LGPD.",
                cancellationToken)
            .ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureLegalDocumentWithPublishedVersionAsync(
        AppDbContext db,
        LegalDocumentType type,
        string defaultTitle,
        string defaultContent,
        CancellationToken cancellationToken)
    {
        var doc = await db.LegalDocuments.FirstOrDefaultAsync(d => d.Type == type, cancellationToken).ConfigureAwait(false);
        if (doc is null)
        {
            doc = new LegalDocumentRecord
            {
                Id = Guid.NewGuid(),
                Type = type,
                Title = defaultTitle,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.LegalDocuments.Add(doc);
        }

        var hasPublished = await db.LegalDocumentVersions
            .AnyAsync(v => v.LegalDocumentId == doc.Id && v.Status == LegalDocumentVersionStatus.Published, cancellationToken)
            .ConfigureAwait(false);
        if (hasPublished)
            return;

        var latest = await db.LegalDocumentVersions
            .Where(v => v.LegalDocumentId == doc.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (latest is not null)
        {
            var siblings = await db.LegalDocumentVersions
                .Where(v => v.LegalDocumentId == doc.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var now = DateTimeOffset.UtcNow;
            foreach (var s in siblings)
            {
                s.Status = LegalDocumentVersionStatus.Draft;
                s.PublishedAt = null;
            }

            var target = siblings.First(x => x.Id == latest.Id);
            target.Status = LegalDocumentVersionStatus.Published;
            target.PublishedAt = now;
        }
        else
        {
            db.LegalDocumentVersions.Add(
                new LegalDocumentVersionRecord
                {
                    Id = Guid.NewGuid(),
                    LegalDocumentId = doc.Id,
                    VersionNumber = 1,
                    Content = defaultContent,
                    Status = LegalDocumentVersionStatus.Published,
                    PublishedAt = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow,
                });
        }
    }

    private static async Task SeedPermissionsAsync(
        AppDbContext db,
        RoleManager<IdentityRole<Guid>> roleManager,
        CancellationToken cancellationToken)
    {
        foreach (var name in ApplicationPermissions.All)
        {
            if (await db.Permissions.AnyAsync(p => p.Name == name, cancellationToken).ConfigureAwait(false))
                continue;
            db.Permissions.Add(new AppPermission { Id = Guid.NewGuid(), Name = name });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var masterRole = await roleManager.FindByNameAsync(SystemRoles.AdministradorMaster).ConfigureAwait(false);
        if (masterRole is null)
            throw new InvalidOperationException($"Role {SystemRoles.AdministradorMaster} was not found after seed.");

        var permissionIds = await db.Permissions.AsNoTracking().Select(p => p.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var permissionId in permissionIds)
        {
            var exists = await db.RolePermissions.AnyAsync(
                    rp => rp.RoleId == masterRole.Id && rp.PermissionId == permissionId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (exists)
                continue;
            db.RolePermissions.Add(new AppRolePermission { RoleId = masterRole.Id, PermissionId = permissionId });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedSampleUsersAsync(AppDbContext db, UserManager<ApplicationUser> userManager, CancellationToken cancellationToken)
    {
        const string samplePassword = "TestPassword123!";

        async Task<ApplicationUser> EnsureUserAsync(string email, string name)
        {
            var user = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
            if (user is not null)
                return user;

            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Name = name,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            var create = await userManager.CreateAsync(user, samplePassword).ConfigureAwait(false);
            if (!create.Succeeded)
                throw new InvalidOperationException($"Failed to create {email}: {string.Join(", ", create.Errors.Select(e => e.Description))}");
            return user;
        }

        var torcedor = await EnsureUserAsync(TestingSeedConstants.TorcedorEmail, "Torcedor Sample").ConfigureAwait(false);
        if (!await userManager.IsInRoleAsync(torcedor, SystemRoles.Torcedor).ConfigureAwait(false))
        {
            var add = await userManager.AddToRoleAsync(torcedor, SystemRoles.Torcedor).ConfigureAwait(false);
            if (!add.Succeeded)
                throw new InvalidOperationException($"Failed to assign Torcedor role: {string.Join(", ", add.Errors.Select(e => e.Description))}");
        }

        var member = await userManager.FindByIdAsync(TestingSeedConstants.SampleMemberUserId.ToString()).ConfigureAwait(false);
        if (member is null)
        {
            member = new ApplicationUser
            {
                Id = TestingSeedConstants.SampleMemberUserId,
                UserName = TestingSeedConstants.MemberEmail,
                Email = TestingSeedConstants.MemberEmail,
                EmailConfirmed = true,
                Name = "Member Sample",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            var createMember = await userManager.CreateAsync(member, samplePassword).ConfigureAwait(false);
            if (!createMember.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to create {TestingSeedConstants.MemberEmail}: {string.Join(", ", createMember.Errors.Select(e => e.Description))}");
        }

        if (!await userManager.IsInRoleAsync(member, SystemRoles.Torcedor).ConfigureAwait(false))
        {
            var add = await userManager.AddToRoleAsync(member, SystemRoles.Torcedor).ConfigureAwait(false);
            if (!add.Succeeded)
                throw new InvalidOperationException($"Failed to assign Torcedor role (member): {string.Join(", ", add.Errors.Select(e => e.Description))}");
        }

        if (!await db.Memberships.AnyAsync(m => m.Id == TestingSeedConstants.SampleMembershipId, cancellationToken).ConfigureAwait(false))
        {
            db.Memberships.Add(
                new MembershipRecord
                {
                    Id = TestingSeedConstants.SampleMembershipId,
                    UserId = member.Id,
                    PlanId = null,
                    Status = MembershipStatus.NaoAssociado,
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = null,
                    NextDueDate = null,
                });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!await db.UserProfiles.AnyAsync(p => p.UserId == TestingSeedConstants.SampleMemberUserId, cancellationToken).ConfigureAwait(false))
        {
            db.UserProfiles.Add(
                new UserProfileRecord
                {
                    UserId = TestingSeedConstants.SampleMemberUserId,
                    Document = "12345678901",
                    BirthDate = new DateOnly(1995, 5, 15),
                    PhotoUrl = null,
                    Address = "Rua do Estádio, 100",
                    AdministrativeNote = null,
                });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
