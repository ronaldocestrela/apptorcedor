namespace AppTorcedor.Identity;

public enum MembershipStatus
{
    NaoAssociado = 0,
    Ativo = 1,
    Inadimplente = 2,
    Suspenso = 3,
    Cancelado = 4,

    /// <summary>Contratação iniciada; aguardando confirmação de pagamento (Parte D.3).</summary>
    PendingPayment = 5,
}
