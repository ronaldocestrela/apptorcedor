using FluentValidation;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.RemoveTenantSetting;

public sealed class RemoveTenantSettingCommandValidator : AbstractValidator<RemoveTenantSettingCommand>
{
    public RemoveTenantSettingCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.SettingId).NotEmpty();
    }
}
