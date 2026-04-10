using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Queries.GetMyMemberBilling;

public sealed record GetMyMemberBillingQuery : IQuery<MemberBillingSubscriptionDto?>;
