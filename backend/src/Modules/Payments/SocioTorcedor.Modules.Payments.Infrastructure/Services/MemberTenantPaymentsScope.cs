using Microsoft.EntityFrameworkCore;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Infrastructure.Persistence;
using SocioTorcedor.Modules.Payments.Infrastructure.Repositories;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

public sealed class MemberTenantPaymentsScope : IMemberTenantPaymentsScope
{
    private readonly TenantPaymentsDbContext _db;

    public IMemberTenantPaymentsRepository Repository { get; }

    public MemberTenantPaymentsScope(string tenantConnectionString)
    {
        var options = new DbContextOptionsBuilder<TenantPaymentsDbContext>()
            .UseSqlServer(
                tenantConnectionString,
                o => o.MigrationsHistoryTable("__EFPaymentsTenantMigrationsHistory"))
            .Options;
        _db = new TenantPaymentsDbContext(options);
        Repository = new MemberTenantPaymentsRepository(_db);
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}

public sealed class MemberTenantPaymentsScopeFactory : IMemberTenantPaymentsScopeFactory
{
    public IMemberTenantPaymentsScope Create(string tenantConnectionString) =>
        new MemberTenantPaymentsScope(tenantConnectionString);
}
