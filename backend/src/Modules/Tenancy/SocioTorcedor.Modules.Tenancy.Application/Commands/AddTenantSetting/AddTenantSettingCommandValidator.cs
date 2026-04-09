using FluentValidation;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.AddTenantSetting;

public sealed class AddTenantSettingCommandValidator : AbstractValidator<AddTenantSettingCommand>
{
    public AddTenantSettingCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Key).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(4000);
    }
}
