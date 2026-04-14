using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminPaymentDetail;

public sealed record GetAdminPaymentDetailQuery(Guid PaymentId) : IRequest<AdminPaymentDetailDto?>;
