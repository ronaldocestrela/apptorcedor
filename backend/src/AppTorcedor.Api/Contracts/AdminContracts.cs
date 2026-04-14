using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AppTorcedor.Identity;

namespace AppTorcedor.Api.Contracts;

public sealed class UpdateMembershipStatusRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MembershipStatus Status { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class UpsertAppConfigurationRequest
{
    public string Value { get; set; } = string.Empty;
}
