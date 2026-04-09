using SocioTorcedor.Modules.Membership.Domain.Enums;

namespace SocioTorcedor.Modules.Membership.Application.DTOs;

public sealed record MemberProfileDto(
    Guid Id,
    string UserId,
    string CpfDigits,
    DateTime DateOfBirth,
    Gender Gender,
    string Phone,
    AddressDto Address,
    MemberStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
