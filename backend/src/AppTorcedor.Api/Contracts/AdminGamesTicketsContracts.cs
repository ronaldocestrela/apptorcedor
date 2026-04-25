using System.ComponentModel.DataAnnotations;

namespace AppTorcedor.Api.Contracts;

public sealed class UpsertGameRequest
{
    [Required]
    [MaxLength(256)]
    public string Opponent { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Competition { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset GameDate { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(2048)]
    public string? OpponentLogoUrl { get; set; }
}

public sealed record OpponentLogoUploadResponse(string Url);

public sealed record OpponentLogoAssetListItemResponse(Guid Id, string Url, DateTimeOffset CreatedAt);

public sealed record OpponentLogoAssetListPageResponse(int TotalCount, IReadOnlyList<OpponentLogoAssetListItemResponse> Items);

public sealed class ReserveTicketRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid GameId { get; set; }
}

public sealed class UpdateTicketRequestStatusRequest
{
    [Required]
    [MaxLength(32)]
    public string RequestStatus { get; set; } = string.Empty;
}
