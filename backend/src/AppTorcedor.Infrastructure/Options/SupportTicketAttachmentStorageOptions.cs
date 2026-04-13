namespace AppTorcedor.Infrastructure.Options;

public sealed class SupportTicketAttachmentStorageOptions
{
    public const string SectionName = "SupportTicketAttachments";

    /// <summary>Absolute root for stored files. Empty = {ContentRoot}/Data/support-attachments.</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>Max size per file in bytes (default 5 MB).</summary>
    public int MaxBytesPerFile { get; set; } = 5 * 1024 * 1024;

    /// <summary>Max files per message (default 5).</summary>
    public int MaxFilesPerMessage { get; set; } = 5;
}
