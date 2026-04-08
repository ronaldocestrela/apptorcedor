using Microsoft.AspNetCore.Identity;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Entities;

public sealed class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
}
