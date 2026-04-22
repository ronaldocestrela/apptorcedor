using AppTorcedor.Application.Modules.Account;

namespace AppTorcedor.Application.Abstractions;

/// <summary>Torcedor self-service: registration and profile (no admin fields; no membership coupling).</summary>
public interface ITorcedorAccountPort
{
    Task<RegisterTorcedorResult> RegisterAsync(RegisterTorcedorRequest request, CancellationToken cancellationToken = default);

    /// <summary>Creates a torcedor account linked to Google with published legal consents (atomic; same rules as <see cref="RegisterAsync"/>).</summary>
    Task<RegisterTorcedorResult> RegisterGoogleUserAsync(
        Guid userId,
        string email,
        string name,
        bool emailVerified,
        string googleSubject,
        IReadOnlyList<Guid> acceptedLegalDocumentVersionIds,
        CancellationToken cancellationToken = default);

    Task<MyProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ProfileUpsertResult> UpsertProfileAsync(Guid userId, MyProfileUpsertDto patch, CancellationToken cancellationToken = default);

    /// <summary>True when profile is missing or required onboarding fields (e.g. document) are empty.</summary>
    Task<bool> RequiresProfileCompletionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Records published terms + privacy consents for a new user (same rules as public registration).</summary>
    Task<bool> RecordInitialConsentsAsync(
        Guid userId,
        IReadOnlyList<Guid> acceptedLegalDocumentVersionIds,
        CancellationToken cancellationToken = default);
}
