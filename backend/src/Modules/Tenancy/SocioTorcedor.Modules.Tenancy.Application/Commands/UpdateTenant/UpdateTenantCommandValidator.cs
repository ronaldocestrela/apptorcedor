using FluentValidation;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.UpdateTenant;

public sealed class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x)
            .Must(c => !string.IsNullOrWhiteSpace(c.Name) || !string.IsNullOrWhiteSpace(c.ConnectionString))
            .WithMessage("At least one of Name or ConnectionString must be provided.");
        When(x => !string.IsNullOrWhiteSpace(x.Name), () =>
        {
            RuleFor(x => x.Name!).MaximumLength(256);
        });
        When(x => !string.IsNullOrWhiteSpace(x.ConnectionString), () =>
        {
            RuleFor(x => x.ConnectionString!).MaximumLength(2048);
        });
    }
}
