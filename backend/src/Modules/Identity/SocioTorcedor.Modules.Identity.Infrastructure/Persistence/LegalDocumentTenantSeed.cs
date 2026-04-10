using Microsoft.EntityFrameworkCore;
using SocioTorcedor.Modules.Identity.Domain.Enums;
using SocioTorcedor.Modules.Identity.Infrastructure.Entities;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Persistence;

public static class LegalDocumentTenantSeed
{
    public static async Task SeedIfEmptyAsync(TenantIdentityDbContext db, CancellationToken cancellationToken)
    {
        if (await db.LegalDocumentVersions.AnyAsync(cancellationToken))
            return;

        var utc = DateTime.UtcNow;
        db.LegalDocumentVersions.AddRange(
            new LegalDocumentVersion
            {
                Id = Guid.NewGuid(),
                Kind = LegalDocumentKind.TermsOfUse,
                VersionNumber = 1,
                Content =
                    "[Substitua pelo texto oficial] Termos de uso do clube. O administrador deve publicar a versão definitiva em /api/legal-documents.",
                PublishedAtUtc = utc,
                IsCurrent = true
            },
            new LegalDocumentVersion
            {
                Id = Guid.NewGuid(),
                Kind = LegalDocumentKind.PrivacyPolicy,
                VersionNumber = 1,
                Content =
                    "[Substitua pelo texto oficial] Política de privacidade do clube. O administrador deve publicar a versão definitiva em /api/legal-documents.",
                PublishedAtUtc = utc,
                IsCurrent = true
            });

        await db.SaveChangesAsync(cancellationToken);
    }
}
