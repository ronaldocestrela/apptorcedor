namespace AppTorcedor.Api.Contracts;

public sealed record IssueDigitalCardRequest(Guid MembershipId);

public sealed record RegenerateDigitalCardRequest(string? Reason);

public sealed record InvalidateDigitalCardRequest(string Reason);
