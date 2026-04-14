namespace AppTorcedor.Application.Abstractions;

public interface IDatabaseConnectivityCheck
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}
