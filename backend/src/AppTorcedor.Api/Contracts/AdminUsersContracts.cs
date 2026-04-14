namespace AppTorcedor.Api.Contracts;

public sealed record SetUserAccountActiveRequest(bool IsActive);

public sealed class UpsertAdminUserProfileRequest
{
    public string? Document { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? PhotoUrl { get; set; }

    public string? Address { get; set; }

    public string? AdministrativeNote { get; set; }
}
