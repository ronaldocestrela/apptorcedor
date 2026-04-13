namespace AppTorcedor.Application.Abstractions;

public interface ISupportTicketAttachmentStorage
{
    /// <summary>Returns storage key on success, or null if validation failed.</summary>
    Task<string?> SaveAsync(
        Guid ticketId,
        Guid messageId,
        byte[] content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Stream? OpenRead(string storageKey);
}
