namespace SocioTorcedor.Modules.Payments.Application.Contracts;

public interface IMemberTenantPaymentsScope : IAsyncDisposable
{
    IMemberTenantPaymentsRepository Repository { get; }
}

public interface IMemberTenantPaymentsScopeFactory
{
    IMemberTenantPaymentsScope Create(string tenantConnectionString);
}
