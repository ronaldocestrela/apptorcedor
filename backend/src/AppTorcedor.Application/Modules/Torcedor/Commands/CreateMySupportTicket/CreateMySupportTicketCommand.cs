using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.CreateMySupportTicket;

public sealed record CreateMySupportTicketCommand(
    Guid UserId,
    string Queue,
    string Subject,
    SupportTicketPriority Priority,
    string? InitialMessage,
    IReadOnlyList<SupportTorcedorAttachmentInput> Attachments) : IRequest<SupportTicketCreateResult>;
