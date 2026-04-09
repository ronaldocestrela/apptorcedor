using Microsoft.Extensions.DependencyInjection;
using SocioTorcedor.Modules.Membership.Infrastructure;

namespace SocioTorcedor.Modules.Membership.Api;

public static class MembershipModule
{
    public static IServiceCollection AddMembershipModule(this IServiceCollection services) =>
        services.AddMembershipInfrastructure();
}
