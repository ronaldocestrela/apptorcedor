using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Membership.Domain.Enums;
using SocioTorcedor.Modules.Membership.Domain.Events;
using SocioTorcedor.Modules.Membership.Domain.Rules;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Domain.Entities;

public sealed class MemberProfile : AggregateRoot
{
    private MemberProfile()
    {
    }

    public string UserId { get; private set; } = null!;

    public string CpfDigits { get; private set; } = null!;

    public DateTime DateOfBirth { get; private set; }

    public Gender Gender { get; private set; }

    public string Phone { get; private set; } = null!;

    public Address Address { get; private set; } = null!;

    public MemberStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public static MemberProfile Create(
        string userId,
        Cpf cpf,
        DateTime dateOfBirth,
        Gender gender,
        string phone,
        Address address,
        Func<bool> cpfAlreadyExists)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User id is required.", nameof(userId));

        var rule = new CpfMustBeUniqueRule(cpfAlreadyExists);
        if (rule.IsBroken())
            throw new BusinessRuleValidationException(rule);

        var normalizedPhone = NormalizePhone(phone);

        var member = new MemberProfile
        {
            UserId = userId.Trim(),
            CpfDigits = cpf.Digits,
            DateOfBirth = dateOfBirth.Date,
            Gender = gender,
            Phone = normalizedPhone,
            Address = address,
            Status = MemberStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        member.AddDomainEvent(new MemberProfileCreatedDomainEvent(member.Id));
        return member;
    }

    public void Update(
        DateTime dateOfBirth,
        Gender gender,
        string phone,
        Address address)
    {
        DateOfBirth = dateOfBirth.Date;
        Gender = gender;
        Phone = NormalizePhone(phone);
        Address = address;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(MemberStatus status) => Status = status;

    private static string NormalizePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone is required.", nameof(phone));

        var trimmed = phone.Trim();
        if (trimmed.Length > 30)
            throw new ArgumentException("Phone is too long.", nameof(phone));

        return trimmed;
    }
}
