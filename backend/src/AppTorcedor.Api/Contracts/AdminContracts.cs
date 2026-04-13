using System.Text.Json.Serialization;
using AppTorcedor.Identity;

namespace AppTorcedor.Api.Contracts;

public sealed class UpdateMembershipStatusRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MembershipStatus Status { get; set; }
}

public sealed class UpsertAppConfigurationRequest
{
    public string Value { get; set; } = string.Empty;
}
