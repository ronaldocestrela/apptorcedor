using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.ReplyMySupportTicket;

public sealed record ReplyMySupportTicketCommand(
    Guid UserId,
    Guid TicketId,
    string Body,
    IReadOnlyList<SupportTorcedorAttachmentInput> Attachments) : IRequest<SupportTicketMutationResult>;
