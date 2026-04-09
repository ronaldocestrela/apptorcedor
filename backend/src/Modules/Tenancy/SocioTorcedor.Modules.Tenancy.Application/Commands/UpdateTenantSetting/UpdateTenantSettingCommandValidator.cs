using FluentValidation;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.UpdateTenantSetting;

public sealed class UpdateTenantSettingCommandValidator : AbstractValidator<UpdateTenantSettingCommand>
{
    public UpdateTenantSettingCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.SettingId).NotEmpty();
        RuleFor(x => x.Value).NotEmpty().MaximumLength(4000);
    }
}
