namespace SocioTorcedor.BuildingBlocks.Shared.Results;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
