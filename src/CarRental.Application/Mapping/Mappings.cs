using CarRental.Application.Contracts.Auth;
using CarRental.Application.Contracts.Cars;
using CarRental.Application.Contracts.Profile;
using CarRental.Application.Contracts.Reservations;
using CarRental.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace CarRental.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class Mappings
{
    public static partial CarResponse ToResponse(this Car car);

    public static partial UserResponse ToResponse(this ApplicationUser user, IReadOnlyList<string> roles);

    public static partial ProfileResponse ToProfileResponse(this ApplicationUser user, IReadOnlyList<string> roles);
    
    [MapProperty("Car.DailyRate", nameof(ReservationResponse.DailyRate))]
    public static partial ReservationResponse ToResponse(this Reservation reservation);
}
