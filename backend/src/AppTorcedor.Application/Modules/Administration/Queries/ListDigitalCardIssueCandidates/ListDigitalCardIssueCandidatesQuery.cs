using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListDigitalCardIssueCandidates;

public sealed record ListDigitalCardIssueCandidatesQuery(int Page, int PageSize) : IRequest<AdminDigitalCardIssueCandidatesPageDto>;
