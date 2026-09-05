using CarRental.Application.Dtos.Reservations;
using FluentValidation;

namespace CarRental.Application.Validators.Reservations;

public sealed class UpdateReservationDtoValidator : AbstractValidator<UpdateReservationDto>
{
    public UpdateReservationDtoValidator()
    {
        RuleFor(x => x.StartDate).ValidPickupDate();
        RuleFor(x => x.EndDate).ValidReturnDate(x => x.StartDate);
        RuleFor(x => x.PickupLocation).ValidPickupLocation();
    }
}
