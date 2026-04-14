using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AppTorcedor.Infrastructure.Persistence;

/// <summary>Design-time factory so migrations always target SQL Server regardless of dev in-memory settings.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var cs =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost,1433;Database=AppTorcedor;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;";
        optionsBuilder.UseSqlServer(cs);
        return new AppDbContext(optionsBuilder.Options);
    }
}
