using AppTorcedor.Identity;

namespace AppTorcedor.Infrastructure.Entities;

/// <summary>Extended profile data (AGENTS.md UserProfile),1:1 with <see cref="ApplicationUser"/>.</summary>
public sealed class UserProfileRecord
{
    public Guid UserId { get; set; }

    public string? Document { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? PhotoUrl { get; set; }

    public string? Address { get; set; }

    /// <summary>Internal administrative note (not shown to the end user in public flows).</summary>
    public string? AdministrativeNote { get; set; }
}
