using System.Security.Cryptography;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account;
using AppTorcedor.Application.Validation;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppTorcedor.Infrastructure.Services.Account;

public sealed class TorcedorAccountService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IRegistrationLegalReadPort legalRead,
    ILgpdAdministrationPort lgpd,
    IEmailSender emailSender,
    IWelcomeEmailComposer welcomeEmailComposer,
    ILogger<TorcedorAccountService> logger) : ITorcedorAccountPort
{
    public async Task<RegisterTorcedorResult> RegisterAsync(RegisterTorcedorRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return RegisterTorcedorResult.Fail("Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Email))
            return RegisterTorcedorResult.Fail("E-mail é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return RegisterTorcedorResult.Fail("Celular é obrigatório.");

        var requirements = await legalRead.GetRequiredPublishedVersionsAsync(cancellationToken).ConfigureAwait(false);
        if (requirements is null)
            return RegisterTorcedorResult.Fail("Documentos legais não estão disponíveis para cadastro.");
        var accepted = request.AcceptedLegalDocumentVersionIds.ToHashSet();
        if (!accepted.Contains(requirements.TermsOfUseVersionId) || !accepted.Contains(requirements.PrivacyPolicyVersionId))
            return RegisterTorcedorResult.Fail("É necessário aceitar os termos de uso e a política de privacidade.");

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
            PhoneNumber = request.PhoneNumber.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var create = await userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
            if (!create.Succeeded)
                return RegisterTorcedorResult.Fail(create.Errors.Select(e => e.Description).ToList());

            var addRole = await userManager.AddToRoleAsync(user, SystemRoles.Torcedor).ConfigureAwait(false);
            if (!addRole.Succeeded)
                return RegisterTorcedorResult.Fail(addRole.Errors.Select(e => e.Description).ToList());

            await lgpd.RecordConsentAsync(user.Id, requirements.TermsOfUseVersionId, null, cancellationToken).ConfigureAwait(false);
            await lgpd.RecordConsentAsync(user.Id, requirements.PrivacyPolicyVersionId, null, cancellationToken).ConfigureAwait(false);

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            await TrySendWelcomeEmailAsync(user.Email!, user.Name, cancellationToken).ConfigureAwait(false);
            return RegisterTorcedorResult.Ok(user.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao registrar torcedor {Email}", request.Email);
            return RegisterTorcedorResult.Fail("Não foi possível concluir o cadastro.");
        }
    }

    public async Task<RegisterTorcedorResult> RegisterGoogleUserAsync(
        Guid userId,
        string email,
        string name,
        bool emailVerified,
        string googleSubject,
        IReadOnlyList<Guid> acceptedLegalDocumentVersionIds,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return RegisterTorcedorResult.Fail("Identificador de usuário inválido.");
        if (string.IsNullOrWhiteSpace(email))
            return RegisterTorcedorResult.Fail("E-mail é obrigatório.");
        if (string.IsNullOrWhiteSpace(name))
            return RegisterTorcedorResult.Fail("Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(googleSubject))
            return RegisterTorcedorResult.Fail("Conta Google inválida.");

        var requirements = await legalRead.GetRequiredPublishedVersionsAsync(cancellationToken).ConfigureAwait(false);
        if (requirements is null)
            return RegisterTorcedorResult.Fail("Documentos legais não estão disponíveis para cadastro.");
        var accepted = acceptedLegalDocumentVersionIds.ToHashSet();
        if (!accepted.Contains(requirements.TermsOfUseVersionId) || !accepted.Contains(requirements.PrivacyPolicyVersionId))
            return RegisterTorcedorResult.Fail("É necessário aceitar os termos de uso e a política de privacidade.");

        if (await userManager.FindByEmailAsync(email).ConfigureAwait(false) is not null)
            return RegisterTorcedorResult.Fail("E-mail já cadastrado.");
        if (await userManager.FindByLoginAsync("Google", googleSubject).ConfigureAwait(false) is not null)
            return RegisterTorcedorResult.Fail("Conta Google já vinculada.");

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = email.Trim(),
            Email = email.Trim(),
            EmailConfirmed = emailVerified,
            Name = name.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var create = await userManager.CreateAsync(user, GenerateInternalPassword()).ConfigureAwait(false);
            if (!create.Succeeded)
                return RegisterTorcedorResult.Fail(create.Errors.Select(e => e.Description).ToList());

            var addRole = await userManager.AddToRoleAsync(user, SystemRoles.Torcedor).ConfigureAwait(false);
            if (!addRole.Succeeded)
                return RegisterTorcedorResult.Fail(addRole.Errors.Select(e => e.Description).ToList());

            var addLogin = await userManager.AddLoginAsync(user, new UserLoginInfo("Google", googleSubject, "Google"))
                .ConfigureAwait(false);
            if (!addLogin.Succeeded)
                return RegisterTorcedorResult.Fail(addLogin.Errors.Select(e => e.Description).ToList());

            await lgpd.RecordConsentAsync(user.Id, requirements.TermsOfUseVersionId, null, cancellationToken).ConfigureAwait(false);
            await lgpd.RecordConsentAsync(user.Id, requirements.PrivacyPolicyVersionId, null, cancellationToken).ConfigureAwait(false);

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            await TrySendWelcomeEmailAsync(user.Email!, user.Name, cancellationToken).ConfigureAwait(false);
            return RegisterTorcedorResult.Ok(user.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao registrar torcedor via Google {Email}", email);
            return RegisterTorcedorResult.Fail("Não foi possível concluir o cadastro.");
        }
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao registrar consentimentos LGPD para {UserId}", userId);
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

    private static string GenerateInternalPassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var b64 = Convert.ToBase64String(bytes);
        return $"{b64}Aa1!";
    }

    private async Task TrySendWelcomeEmailAsync(string toEmail, string displayName, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("E-mail de boas-vindas: iniciando para {Email}", toEmail);
            var message = await welcomeEmailComposer
                .ComposeWelcomeAsync(toEmail, displayName, cancellationToken)
                .ConfigureAwait(false);
            await emailSender.SendAsync(message, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("E-mail de boas-vindas: envio concluído para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Falha ao enviar e-mail de boas-vindas para {Email}. Com Provider=Resend, confirme domínio/remetente verificados no painel Resend e Email:Resend:FromAddress.",
                toEmail);
        }
    }
}
