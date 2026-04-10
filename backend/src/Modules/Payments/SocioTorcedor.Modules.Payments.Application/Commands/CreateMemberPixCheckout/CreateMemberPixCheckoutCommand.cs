using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Commands.CreateMemberPixCheckout;

public sealed record CreateMemberPixCheckoutCommand(Guid MemberPlanId) : ICommand<MemberPixCheckoutDto>;
