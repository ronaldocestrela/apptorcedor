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
}

public sealed class ReserveTicketRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid GameId { get; set; }
}
