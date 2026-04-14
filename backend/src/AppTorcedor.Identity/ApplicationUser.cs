using Microsoft.AspNetCore.Identity;

namespace AppTorcedor.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Display name (per AGENTS.md ApplicationUser.Name).</summary>
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
