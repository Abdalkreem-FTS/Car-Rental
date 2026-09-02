using CarRental.Application.Dtos.Auth;
using CarRental.Application.Dtos.Cars;
using CarRental.Application.Dtos.Profile;
using CarRental.Application.Dtos.Reservations;
using CarRental.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace CarRental.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class Mappings
{
    public static partial CarDto ToResponse(this Car car);

    public static partial UserDto ToResponse(this ApplicationUser user, IReadOnlyList<string> roles);

    public static partial ProfileDto ToProfileDto(this ApplicationUser user, IReadOnlyList<string> roles);

    [MapperIgnoreTarget(nameof(ReservationDto.PreviousTotalPrice))]
    public static partial ReservationDto ToResponse(this Reservation reservation);
}
