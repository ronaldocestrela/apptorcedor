using System.Linq;

namespace AppTorcedor.Identity;

/// <summary>Role names aligned with AGENTS.md profiles.</summary>
public static class SystemRoles
{
    public const string AdministradorMaster = "Administrador Master";
    public const string Administrador = "Administrador";
    public const string Financeiro = "Financeiro";
    public const string Atendimento = "Atendimento";
    public const string Marketing = "Marketing";
    public const string Operador = "Operador";
    public const string Torcedor = "Torcedor";

    public static IReadOnlyList<string> All { get; } =
    [
        AdministradorMaster,
        Administrador,
        Financeiro,
        Atendimento,
        Marketing,
        Operador,
        Torcedor,
    ];

    /// <summary>Roles that can operate the club backoffice (excludes <see cref="Torcedor"/>).</summary>
    public static IReadOnlyList<string> AllExceptTorcedor { get; } =
    [
        AdministradorMaster,
        Administrador,
        Financeiro,
        Atendimento,
        Marketing,
        Operador,
    ];

    public static bool IsAssignableStaffRole(string roleName) =>
        AllExceptTorcedor.Any(r => r == roleName);
}
