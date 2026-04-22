using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account;
using AppTorcedor.Application.Validation;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Account;

public sealed class TorcedorAccountService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IRegistrationLegalReadPort legalRead,
    ILgpdAdministrationPort lgpd) : ITorcedorAccountPort
{
    public async Task<RegisterTorcedorResult> RegisterAsync(RegisterTorcedorRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return RegisterTorcedorResult.Fail("Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Email))
            return RegisterTorcedorResult.Fail("E-mail é obrigatório.");

        if (!await HasValidRegistrationAcceptancesAsync(request.AcceptedLegalDocumentVersionIds, cancellationToken).ConfigureAwait(false))
        {
            var requirements = await legalRead.GetRequiredPublishedVersionsAsync(cancellationToken).ConfigureAwait(false);
            if (requirements is null)
                return RegisterTorcedorResult.Fail("Documentos legais não estão disponíveis para cadastro.");
            return RegisterTorcedorResult.Fail("É necessário aceitar os termos de uso e a política de privacidade.");
        }

        var existing = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (existing is not null)
            return RegisterTorcedorResult.Fail("E-mail já cadastrado.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            Name = request.Name.Trim(),
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var create = await userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!create.Succeeded)
            return RegisterTorcedorResult.Fail(create.Errors.Select(e => e.Description).ToList());

        var addRole = await userManager.AddToRoleAsync(user, SystemRoles.Torcedor).ConfigureAwait(false);
        if (!addRole.Succeeded)
        {
            await userManager.DeleteAsync(user).ConfigureAwait(false);
            return RegisterTorcedorResult.Fail(addRole.Errors.Select(e => e.Description).ToList());
        }

        if (!await RecordInitialConsentsAsync(user.Id, request.AcceptedLegalDocumentVersionIds, cancellationToken).ConfigureAwait(false))
        {
            await userManager.DeleteAsync(user).ConfigureAwait(false);
            return RegisterTorcedorResult.Fail("Não foi possível registrar os consentimentos LGPD.");
        }

        return RegisterTorcedorResult.Ok(user.Id);
    }

    public async Task<bool> RecordInitialConsentsAsync(
        Guid userId,
        IReadOnlyList<Guid> acceptedLegalDocumentVersionIds,
        CancellationToken cancellationToken = default)
    {
        if (!await HasValidRegistrationAcceptancesAsync(acceptedLegalDocumentVersionIds, cancellationToken).ConfigureAwait(false))
            return false;

        var requirements = await legalRead.GetRequiredPublishedVersionsAsync(cancellationToken).ConfigureAwait(false);
        if (requirements is null)
            return false;

        try
        {
            await lgpd.RecordConsentAsync(userId, requirements.TermsOfUseVersionId, null, cancellationToken).ConfigureAwait(false);
            await lgpd.RecordConsentAsync(userId, requirements.PrivacyPolicyVersionId, null, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> HasValidRegistrationAcceptancesAsync(
        IReadOnlyList<Guid> acceptedLegalDocumentVersionIds,
        CancellationToken cancellationToken)
    {
        var requirements = await legalRead.GetRequiredPublishedVersionsAsync(cancellationToken).ConfigureAwait(false);
        if (requirements is null)
            return false;
        var accepted = acceptedLegalDocumentVersionIds.ToHashSet();
        return accepted.Contains(requirements.TermsOfUseVersionId) && accepted.Contains(requirements.PrivacyPolicyVersionId);
    }

    public async Task<MyProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false))
            return null;

        var entity = await db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
            return new MyProfileDto(null, null, null, null);

        return new MyProfileDto(entity.Document, entity.BirthDate, entity.PhotoUrl, entity.Address);
    }

    public async Task<ProfileUpsertResult> UpsertProfileAsync(
        Guid userId,
        MyProfileUpsertDto patch,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false))
            return ProfileUpsertResult.NotFound();

        var entity = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            entity = new UserProfileRecord { UserId = userId };
            db.UserProfiles.Add(entity);
        }

        if (patch.Document is not null)
        {
            if (string.IsNullOrWhiteSpace(patch.Document))
                entity.Document = null;
            else if (!CpfNumber.TryParse(patch.Document, out var normalized))
                return ProfileUpsertResult.InvalidDocument();
            else
            {
                var taken = await db.UserProfiles
                    .AsNoTracking()
                    .AnyAsync(p => p.Document == normalized && p.UserId != userId, cancellationToken)
                    .ConfigureAwait(false);
                if (taken)
                    return ProfileUpsertResult.DocumentAlreadyInUse();
                entity.Document = normalized;
            }
        }

        if (patch.BirthDate is not null)
            entity.BirthDate = patch.BirthDate;
        if (patch.PhotoUrl is not null)
            entity.PhotoUrl = string.IsNullOrWhiteSpace(patch.PhotoUrl) ? null : patch.PhotoUrl.Trim();
        if (patch.Address is not null)
            entity.Address = string.IsNullOrWhiteSpace(patch.Address) ? null : patch.Address.Trim();

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (SqlServerUniqueIndexViolation.IsUniqueConstraintOnSave(ex))
        {
            return ProfileUpsertResult.DocumentAlreadyInUse();
        }

        return ProfileUpsertResult.Ok();
    }

    public async Task<bool> RequiresProfileCompletionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false))
            return false;

        var entity = await db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
            return true;
        return string.IsNullOrWhiteSpace(entity.Document);
    }
}
