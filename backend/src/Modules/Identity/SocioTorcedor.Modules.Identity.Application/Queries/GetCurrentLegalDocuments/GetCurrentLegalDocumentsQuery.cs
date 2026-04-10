using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Identity.Application.DTOs;

namespace SocioTorcedor.Modules.Identity.Application.Queries.GetCurrentLegalDocuments;

public sealed record GetCurrentLegalDocumentsQuery : IQuery<CurrentLegalDocumentsDto>;
