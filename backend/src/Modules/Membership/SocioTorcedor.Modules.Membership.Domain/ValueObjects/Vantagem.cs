namespace SocioTorcedor.Modules.Membership.Domain.ValueObjects;

/// <summary>
/// Vantagem de um plano de sócio (value object; persistida em JSON, sem entidade própria).
/// </summary>
public sealed class Vantagem : IEquatable<Vantagem>
{
    public string Descricao { get; set; } = null!;

    private Vantagem()
    {
    }

    public static Vantagem Create(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Vantagem description is required.", nameof(descricao));

        return new Vantagem { Descricao = descricao.Trim() };
    }

    public bool Equals(Vantagem? other) =>
        other is not null && string.Equals(Descricao, other.Descricao, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is Vantagem other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Descricao);
}
