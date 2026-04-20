namespace AppTorcedor.Application.Abstractions;

public sealed record UploadOpponentLogoResult(string Url);

public sealed record OpponentLogoAssetListItemDto(Guid Id, string Url, DateTimeOffset CreatedAt);

public sealed record OpponentLogoAssetListPageDto(int TotalCount, IReadOnlyList<OpponentLogoAssetListItemDto> Items);

/// <summary>Admin library of uploaded opponent logos (for reuse when creating games).</summary>
public interface IOpponentLogoLibraryAdminPort
{
    Task<UploadOpponentLogoResult?> UploadAndRegisterAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<OpponentLogoAssetListPageDto> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
