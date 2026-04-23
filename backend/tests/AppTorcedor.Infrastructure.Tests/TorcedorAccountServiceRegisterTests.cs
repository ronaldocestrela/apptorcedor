using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account;
using AppTorcedor.Application.Modules.Lgpd;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Services.Account;
using AppTorcedor.Infrastructure.Services.Lgpd;
using AppTorcedor.Infrastructure.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using AppDocType = AppTorcedor.Application.Modules.Lgpd.LegalDocumentType;
using InfraDocType = AppTorcedor.Infrastructure.Entities.LegalDocumentType;

namespace AppTorcedor.Infrastructure.Tests;

/// <summary>
/// Testa <see cref="TorcedorAccountService.RecordInitialConsentsAsync"/> diretamente (sem subir a API).
/// Verifica que os consentimentos LGPD são gravados — ou não — de acordo com a situação.
/// </summary>
public sealed class TorcedorAccountServiceRegisterTests
{
    // ─── Infraestrutura de apoio ─────────────────────────────────────────────────

    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    /// <summary>Insere documentos legais com uma versão publicada cada e devolve os IDs das versões.</summary>
    private static async Task<(Guid TermsVersionId, Guid PrivacyVersionId)> SeedPublishedLegalDocsAsync(AppDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        var termsDocId = Guid.NewGuid();
        var privacyDocId = Guid.NewGuid();
        var termsVersionId = Guid.NewGuid();
        var privacyVersionId = Guid.NewGuid();

        db.LegalDocuments.AddRange(
            new LegalDocumentRecord
            {
                Id = termsDocId,
                Type = InfraDocType.TermsOfUse,
                Title = "Termos de uso",
                CreatedAt = now,
            },
            new LegalDocumentRecord
            {
                Id = privacyDocId,
                Type = InfraDocType.PrivacyPolicy,
                Title = "Política de privacidade",
                CreatedAt = now,
            });

        db.LegalDocumentVersions.AddRange(
            new LegalDocumentVersionRecord
            {
                Id = termsVersionId,
                LegalDocumentId = termsDocId,
                VersionNumber = 1,
                Content = "Texto dos termos.",
                Status = LegalDocumentVersionStatus.Published,
                PublishedAt = now,
                CreatedAt = now,
            },
            new LegalDocumentVersionRecord
            {
                Id = privacyVersionId,
                LegalDocumentId = privacyDocId,
                VersionNumber = 1,
                Content = "Texto da política.",
                Status = LegalDocumentVersionStatus.Published,
                PublishedAt = now,
                CreatedAt = now,
            });

        await db.SaveChangesAsync();
        return (termsVersionId, privacyVersionId);
    }

    /// <summary>Insere um ApplicationUser diretamente no banco sem usar o UserManager.</summary>
    private static async Task<Guid> AddUserAsync(AppDbContext db)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = $"u-{userId:N}@test",
            NormalizedUserName = $"U-{userId:N}@TEST",
            Email = $"u-{userId:N}@test",
            NormalizedEmail = $"U-{userId:N}@TEST",
            EmailConfirmed = true,
            Name = "Test User",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
        return userId;
    }

    /// <summary>
    /// Cria o SUT usando serviços reais (sem interceptor de auditoria — contexto direto).
    /// UserManager não é necessário para RecordInitialConsentsAsync.
    /// </summary>
    private static TorcedorAccountService BuildSut(AppDbContext db) =>
        new(db, null!, new RegistrationLegalReadService(db), new LgpdAdministrationService(db, null!, null!), new NoopEmailSender(), NullLogger<TorcedorAccountService>.Instance);

    // ─── RegisterAsync + transação (SQLite — provider InMemory não garante rollback) ─

    [Fact]
    public async Task RegisterAsync_rolls_back_user_creation_when_consent_recording_throws()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await db.Database.EnsureCreatedAsync();
        if (!await roleManager.RoleExistsAsync(SystemRoles.Torcedor))
            await roleManager.CreateAsync(new IdentityRole<Guid>(SystemRoles.Torcedor));

        var (termsVerId, privacyVerId) = await SeedPublishedLegalDocsAsync(db);
        var lgpdInner = new LgpdAdministrationService(db, userManager, null!);
        var lgpd = new SecondCallThrowsLgpdWrapper(lgpdInner);
        var sut = new TorcedorAccountService(
            db,
            userManager,
            new RegistrationLegalReadService(db),
            lgpd,
            new NoopEmailSender(),
            NullLogger<TorcedorAccountService>.Instance);

        var req = new RegisterTorcedorRequest(
            "Test User",
            "rollback-test@test.local",
            "Password123!",
            null,
            [termsVerId, privacyVerId]);

        var result = await sut.RegisterAsync(req, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Null(await userManager.FindByEmailAsync("rollback-test@test.local"));
        Assert.Empty(await db.UserConsents.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task RegisterAsync_sends_welcome_email_after_successful_commit()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await db.Database.EnsureCreatedAsync();
        if (!await roleManager.RoleExistsAsync(SystemRoles.Torcedor))
            await roleManager.CreateAsync(new IdentityRole<Guid>(SystemRoles.Torcedor));

        var (termsVerId, privacyVerId) = await SeedPublishedLegalDocsAsync(db);
        var capturer = new CapturingEmailSender();
        var sut = new TorcedorAccountService(
            db,
            userManager,
            new RegistrationLegalReadService(db),
            new LgpdAdministrationService(db, userManager, null!),
            capturer,
            NullLogger<TorcedorAccountService>.Instance);

        var req = new RegisterTorcedorRequest(
            "Welcome User",
            "welcome-form@test.local",
            "Password123!",
            null,
            [termsVerId, privacyVerId]);

        var result = await sut.RegisterAsync(req, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(await userManager.FindByEmailAsync("welcome-form@test.local"));
        Assert.Single(capturer.Sent);
        var msg = capturer.Sent[0];
        Assert.Equal("welcome-form@test.local", msg.To);
        Assert.Equal("Bem-vindo ao sócio torcedor", msg.Subject);
        Assert.Contains("Welcome User", msg.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Welcome User", msg.PlainTextBody ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterGoogleUserAsync_sends_welcome_email_after_successful_commit()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await db.Database.EnsureCreatedAsync();
        if (!await roleManager.RoleExistsAsync(SystemRoles.Torcedor))
            await roleManager.CreateAsync(new IdentityRole<Guid>(SystemRoles.Torcedor));

        var (termsVerId, privacyVerId) = await SeedPublishedLegalDocsAsync(db);
        var capturer = new CapturingEmailSender();
        var sut = new TorcedorAccountService(
            db,
            userManager,
            new RegistrationLegalReadService(db),
            new LgpdAdministrationService(db, userManager, null!),
            capturer,
            NullLogger<TorcedorAccountService>.Instance);

        var userId = Guid.NewGuid();
        var result = await sut.RegisterGoogleUserAsync(
            userId,
            "google-welcome@test.local",
            "Maria Google",
            true,
            "google-subject-123",
            [termsVerId, privacyVerId],
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(await userManager.FindByEmailAsync("google-welcome@test.local"));
        Assert.Single(capturer.Sent);
        var msg = capturer.Sent[0];
        Assert.Equal("google-welcome@test.local", msg.To);
        Assert.Contains("Maria Google", msg.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterAsync_succeeds_when_welcome_email_throws()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await db.Database.EnsureCreatedAsync();
        if (!await roleManager.RoleExistsAsync(SystemRoles.Torcedor))
            await roleManager.CreateAsync(new IdentityRole<Guid>(SystemRoles.Torcedor));

        var (termsVerId, privacyVerId) = await SeedPublishedLegalDocsAsync(db);
        var sut = new TorcedorAccountService(
            db,
            userManager,
            new RegistrationLegalReadService(db),
            new LgpdAdministrationService(db, userManager, null!),
            new ThrowingEmailSender(),
            NullLogger<TorcedorAccountService>.Instance);

        var req = new RegisterTorcedorRequest(
            "User MailFail",
            "mail-fail@test.local",
            "Password123!",
            null,
            [termsVerId, privacyVerId]);

        var result = await sut.RegisterAsync(req, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(await userManager.FindByEmailAsync("mail-fail@test.local"));
    }

    // ─── Caminho feliz ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordInitialConsents_stores_both_consent_records_in_database()
    {
        await using var db = CreateDb();
        var (termsVerId, privacyVerId) = await SeedPublishedLegalDocsAsync(db);
        var userId = await AddUserAsync(db);
        var sut = BuildSut(db);

        var result = await sut.RecordInitialConsentsAsync(
            userId, [termsVerId, privacyVerId], CancellationToken.None);

        Assert.True(result);

        var consents = await db.UserConsents.AsNoTracking()
            .Where(c => c.UserId == userId)
            .ToListAsync();

        Assert.Equal(2, consents.Count);
        Assert.Contains(consents, c => c.LegalDocumentVersionId == termsVerId);
        Assert.Contains(consents, c => c.LegalDocumentVersionId == privacyVerId);
    }

    [Fact]
    public async Task RecordInitialConsents_sets_accepted_at_to_utc_now()
    {
        await using var db = CreateDb();
        var (termsVerId, privacyVerId) = await SeedPublishedLegalDocsAsync(db);
        var userId = await AddUserAsync(db);
        var sut = BuildSut(db);
        var before = DateTimeOffset.UtcNow;

        await sut.RecordInitialConsentsAsync(userId, [termsVerId, privacyVerId], CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        var consents = await db.UserConsents.AsNoTracking()
            .Where(c => c.UserId == userId)
            .ToListAsync();

        foreach (var c in consents)
            Assert.InRange(c.AcceptedAt, before, after);
    }

    // ─── IDs de aceite inválidos / incompletos ────────────────────────────────────

    [Fact]
    public async Task RecordInitialConsents_returns_false_when_accepted_ids_are_empty()
    {
        await using var db = CreateDb();
        await SeedPublishedLegalDocsAsync(db);
        var userId = await AddUserAsync(db);
        var sut = BuildSut(db);

        var result = await sut.RecordInitialConsentsAsync(
            userId, [], CancellationToken.None);

        Assert.False(result);
        Assert.Empty(await db.UserConsents.AsNoTracking().Where(c => c.UserId == userId).ToListAsync());
    }

    [Fact]
    public async Task RecordInitialConsents_returns_false_when_accepted_ids_do_not_match_published_versions()
    {
        await using var db = CreateDb();
        await SeedPublishedLegalDocsAsync(db);
        var userId = await AddUserAsync(db);
        var sut = BuildSut(db);

        var result = await sut.RecordInitialConsentsAsync(
            userId, [Guid.NewGuid(), Guid.NewGuid()], CancellationToken.None);

        Assert.False(result);
        Assert.Empty(await db.UserConsents.AsNoTracking().Where(c => c.UserId == userId).ToListAsync());
    }

    [Fact]
    public async Task RecordInitialConsents_returns_false_when_only_terms_is_provided()
    {
        await using var db = CreateDb();
        var (termsVerId, _) = await SeedPublishedLegalDocsAsync(db);
        var userId = await AddUserAsync(db);
        var sut = BuildSut(db);

        var result = await sut.RecordInitialConsentsAsync(
            userId, [termsVerId], CancellationToken.None);

        Assert.False(result);
        Assert.Empty(await db.UserConsents.AsNoTracking().Where(c => c.UserId == userId).ToListAsync());
    }

    [Fact]
    public async Task RecordInitialConsents_returns_false_when_only_privacy_is_provided()
    {
        await using var db = CreateDb();
        var (_, privacyVerId) = await SeedPublishedLegalDocsAsync(db);
        var userId = await AddUserAsync(db);
        var sut = BuildSut(db);

        var result = await sut.RecordInitialConsentsAsync(
            userId, [privacyVerId], CancellationToken.None);

        Assert.False(result);
        Assert.Empty(await db.UserConsents.AsNoTracking().Where(c => c.UserId == userId).ToListAsync());
    }

    // ─── Documentos legais não configurados ──────────────────────────────────────

    [Fact]
    public async Task RecordInitialConsents_returns_false_when_no_legal_documents_exist_in_database()
    {
        await using var db = CreateDb();
        // banco vazio — sem documentos legais
        var userId = await AddUserAsync(db);
        var sut = BuildSut(db);

        var result = await sut.RecordInitialConsentsAsync(
            userId, [Guid.NewGuid(), Guid.NewGuid()], CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task RecordInitialConsents_returns_false_when_documents_exist_but_no_version_is_published()
    {
        await using var db = CreateDb();
        var now = DateTimeOffset.UtcNow;
        var docId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        db.LegalDocuments.Add(new LegalDocumentRecord
        {
            Id = docId,
            Type = InfraDocType.TermsOfUse,
            Title = "Termos",
            CreatedAt = now,
        });
        db.LegalDocumentVersions.Add(new LegalDocumentVersionRecord
        {
            Id = versionId,
            LegalDocumentId = docId,
            VersionNumber = 1,
            Content = "Rascunho.",
            Status = LegalDocumentVersionStatus.Draft, // não publicado
            PublishedAt = null,
            CreatedAt = now,
        });
        await db.SaveChangesAsync();

        var userId = await AddUserAsync(db);
        var sut = BuildSut(db);

        var result = await sut.RecordInitialConsentsAsync(
            userId, [versionId, Guid.NewGuid()], CancellationToken.None);

        Assert.False(result);
    }

    // ─── Falha na porta de LGPD ───────────────────────────────────────────────────

    [Fact]
    public async Task RecordInitialConsents_returns_false_when_lgpd_port_throws()
    {
        await using var db = CreateDb();
        var (termsVerId, privacyVerId) = await SeedPublishedLegalDocsAsync(db);
        var userId = await AddUserAsync(db);

        var sut = new TorcedorAccountService(
            db,
            null!,
            new RegistrationLegalReadService(db),
            new AlwaysThrowingLgpdPort(),
            new NoopEmailSender(),
            NullLogger<TorcedorAccountService>.Instance);

        var result = await sut.RecordInitialConsentsAsync(
            userId, [termsVerId, privacyVerId], CancellationToken.None);

        Assert.False(result);
    }

    // ─── Doubles ─────────────────────────────────────────────────────────────────

    private sealed class ThrowingEmailSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated e-mail provider failure.");
    }

    /// <summary>Delega ao serviço real e falha na segunda gravação de consentimento (para exercitar rollback).</summary>
    private sealed class SecondCallThrowsLgpdWrapper(ILgpdAdministrationPort inner) : ILgpdAdministrationPort
    {
        private int _recordConsentCallCount;

        public Task RecordConsentAsync(Guid userId, Guid documentVersionId, string? clientIp, CancellationToken cancellationToken = default)
        {
            _recordConsentCallCount++;
            if (_recordConsentCallCount >= 2)
                throw new InvalidOperationException("Simulated failure on second LGPD consent.");
            return inner.RecordConsentAsync(userId, documentVersionId, clientIp, cancellationToken);
        }

        public Task<IReadOnlyList<LegalDocumentListItemDto>> ListDocumentsAsync(CancellationToken cancellationToken = default) =>
            inner.ListDocumentsAsync(cancellationToken);

        public Task<LegalDocumentDetailDto?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default) =>
            inner.GetDocumentAsync(id, cancellationToken);

        public Task<LegalDocumentDetailDto> CreateDocumentAsync(AppDocType type, string title, CancellationToken cancellationToken = default) =>
            inner.CreateDocumentAsync(type, title, cancellationToken);

        public Task<LegalDocumentVersionDetailDto> AddVersionAsync(Guid documentId, string content, CancellationToken cancellationToken = default) =>
            inner.AddVersionAsync(documentId, content, cancellationToken);

        public Task PublishVersionAsync(Guid versionId, CancellationToken cancellationToken = default) =>
            inner.PublishVersionAsync(versionId, cancellationToken);

        public Task<IReadOnlyList<UserConsentRowDto>> ListConsentsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            inner.ListConsentsForUserAsync(userId, cancellationToken);

        public Task<PrivacyOperationResultDto> ExportUserDataAsync(Guid subjectUserId, Guid requestedByUserId, CancellationToken cancellationToken = default) =>
            inner.ExportUserDataAsync(subjectUserId, requestedByUserId, cancellationToken);

        public Task<PrivacyOperationResultDto> AnonymizeUserAsync(Guid subjectUserId, Guid requestedByUserId, CancellationToken cancellationToken = default) =>
            inner.AnonymizeUserAsync(subjectUserId, requestedByUserId, cancellationToken);
    }

    /// <summary>ILgpdAdministrationPort que lança exceção em qualquer chamada a RecordConsentAsync.</summary>
    private sealed class AlwaysThrowingLgpdPort : ILgpdAdministrationPort
    {
        public Task RecordConsentAsync(Guid userId, Guid documentVersionId, string? clientIp, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Falha simulada na porta LGPD.");

        public Task<IReadOnlyList<LegalDocumentListItemDto>> ListDocumentsAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<LegalDocumentDetailDto?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<LegalDocumentDetailDto> CreateDocumentAsync(AppDocType type, string title, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<LegalDocumentVersionDetailDto> AddVersionAsync(Guid documentId, string content, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task PublishVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<UserConsentRowDto>> ListConsentsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<PrivacyOperationResultDto> ExportUserDataAsync(Guid subjectUserId, Guid requestedByUserId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<PrivacyOperationResultDto> AnonymizeUserAsync(Guid subjectUserId, Guid requestedByUserId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
