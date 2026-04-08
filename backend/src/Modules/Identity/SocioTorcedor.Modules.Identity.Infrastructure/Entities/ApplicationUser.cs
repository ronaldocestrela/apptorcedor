using Microsoft.AspNetCore.Identity;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; }
}
