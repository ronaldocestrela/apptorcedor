using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Identity.Domain.Enums;

namespace SocioTorcedor.Modules.Identity.Application.Commands.PublishLegalDocumentVersion;

public sealed record PublishLegalDocumentVersionCommand(LegalDocumentKind Kind, string Content)
    : ICommand;
