using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminDashboard;

public sealed record GetAdminDashboardQuery : IRequest<AdminDashboardDto>;
