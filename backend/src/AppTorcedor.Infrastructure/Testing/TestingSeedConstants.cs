namespace AppTorcedor.Infrastructure.Testing;

/// <summary>Stable identifiers for optional Testing-environment sample data (see <c>Testing:SeedSampleUsers</c>).</summary>
public static class TestingSeedConstants
{
    public const string TorcedorEmail = "torcedor@test.local";
    public const string MemberEmail = "member@test.local";

    /// <summary>CPF matematicamente válido usado no seed do membro de teste (normalizado, 11 dígitos).</summary>
    public const string SampleMemberCpf = "11144477735";

    /// <summary>CPF válido distinto de <see cref="SampleMemberCpf"/> para cadastros de torcedor em testes (evita conflito de unicidade).</summary>
    public const string SampleNewUserCpf = "39053344705";

    public static readonly Guid SampleMemberUserId = new("f6f6d6c8-0a1a-4c0a-8f0a-000000000001");
    public static readonly Guid SampleMembershipId = new("f6f6d6c8-0a1a-4c0a-8f0a-000000000002");
}
