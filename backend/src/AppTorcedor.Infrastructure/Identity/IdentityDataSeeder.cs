using AppTorcedor.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppTorcedor.Infrastructure.Identity;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var env = services.GetRequiredService<IHostEnvironment>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in SystemRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
                continue;
            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName) { Id = Guid.NewGuid() })
                .ConfigureAwait(false);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to create role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var email = configuration["Seed:AdminMaster:Email"] ?? "admin@torcedor.local";
        var password =
            configuration["Seed:AdminMaster:Password"]
            ?? Environment.GetEnvironmentVariable("ADMIN_MASTER_INITIAL_PASSWORD");

        if (string.IsNullOrWhiteSpace(password))
        {
            if (env.IsDevelopment() || env.IsEnvironment("Testing"))
                password = "ChangeMe_Integration1!";
            else
                throw new InvalidOperationException(
                    "ADMIN_MASTER_INITIAL_PASSWORD or Seed:AdminMaster:Password must be set outside Development/Testing.");
        }

        var admin = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Name = "Administrador Master",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            var create = await userManager.CreateAsync(admin, password).ConfigureAwait(false);
            if (!create.Succeeded)
                throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", create.Errors.Select(e => e.Description))}");
        }

        if (!await userManager.IsInRoleAsync(admin, SystemRoles.AdministradorMaster).ConfigureAwait(false))
        {
            var addRole = await userManager.AddToRoleAsync(admin, SystemRoles.AdministradorMaster).ConfigureAwait(false);
            if (!addRole.Succeeded)
                throw new InvalidOperationException($"Failed to assign Administrador Master: {string.Join(", ", addRole.Errors.Select(e => e.Description))}");
        }
    }
}
