using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Application.DTOs;

public static class MemberProfileMappings
{
    public static AddressDto ToDto(this Address address) =>
        new(
            address.Street,
            address.Number,
            address.Complement,
            address.Neighborhood,
            address.City,
            address.State,
            address.ZipCode);

    public static MemberProfileDto ToDto(this MemberProfile profile) =>
        new(
            profile.Id,
            profile.UserId,
            profile.CpfDigits,
            profile.DateOfBirth,
            profile.Gender,
            profile.Phone,
            profile.Address.ToDto(),
            profile.Status,
            profile.CreatedAt,
            profile.UpdatedAt);
}
