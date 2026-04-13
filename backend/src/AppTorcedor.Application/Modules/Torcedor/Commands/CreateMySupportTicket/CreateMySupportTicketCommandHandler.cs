using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.CreateMySupportTicket;

public sealed class CreateMySupportTicketCommandHandler(ISupportTorcedorPort port)
    : IRequestHandler<CreateMySupportTicketCommand, SupportTicketCreateResult>
{
    public Task<SupportTicketCreateResult> Handle(CreateMySupportTicketCommand request, CancellationToken cancellationToken) =>
        port.CreateMyTicketAsync(
            request.UserId,
            request.Queue,
            request.Subject,
            request.Priority,
            request.InitialMessage,
            request.Attachments,
            cancellationToken);
}
