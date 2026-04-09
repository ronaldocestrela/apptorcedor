using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Membership.Application.DTOs;
using SocioTorcedor.Modules.Membership.Domain.Enums;

namespace SocioTorcedor.Modules.Membership.Application.Commands.UpdateMemberProfile;

public sealed record UpdateMemberProfileCommand(
    DateTime DateOfBirth,
    Gender Gender,
    string Phone,
    string Street,
    string Number,
    string? Complement,
    string Neighborhood,
    string City,
    string State,
    string ZipCode) : ICommand<MemberProfileDto>;
