namespace AppTorcedor.Identity;

/// <summary>Status de solicitação/ emissão do ingresso (separado do <see cref="TicketStatus"/> do provedor/ciclo de uso).</summary>
public enum TicketRequestStatus
{
    Pending = 1,
    Issued = 2,
}
