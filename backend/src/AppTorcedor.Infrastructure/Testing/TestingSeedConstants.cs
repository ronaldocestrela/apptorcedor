namespace AppTorcedor.Infrastructure.Testing;

/// <summary>Stable identifiers for optional Testing-environment sample data (see <c>Testing:SeedSampleUsers</c>).</summary>
public static class TestingSeedConstants
{
    public const string TorcedorEmail = "torcedor@test.local";
    public const string MemberEmail = "member@test.local";

    public static readonly Guid SampleMemberUserId = new("f6f6d6c8-0a1a-4c0a-8f0a-000000000001");
    public static readonly Guid SampleMembershipId = new("f6f6d6c8-0a1a-4c0a-8f0a-000000000002");
}
