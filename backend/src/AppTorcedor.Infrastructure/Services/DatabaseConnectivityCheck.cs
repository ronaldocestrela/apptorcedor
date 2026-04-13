using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services;

public sealed class DatabaseConnectivityCheck(AppDbContext db) : IDatabaseConnectivityCheck
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) =>
        db.Database.CanConnectAsync(cancellationToken);
}
