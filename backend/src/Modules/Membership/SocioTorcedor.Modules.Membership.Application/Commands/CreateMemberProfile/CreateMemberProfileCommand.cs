using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Membership.Application.DTOs;
using SocioTorcedor.Modules.Membership.Domain.Enums;

namespace SocioTorcedor.Modules.Membership.Application.Commands.CreateMemberProfile;

public sealed record CreateMemberProfileCommand(
    string Cpf,
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
