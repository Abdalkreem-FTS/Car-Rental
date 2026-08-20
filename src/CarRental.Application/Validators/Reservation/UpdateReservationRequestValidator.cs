using CarRental.Application.Contracts.Reservations;
using FluentValidation;

namespace CarRental.Application.Validators.Reservation;

public sealed class UpdateReservationRequestValidator : AbstractValidator<UpdateReservationRequest>
{
    public UpdateReservationRequestValidator()
    {
        RuleFor(x => x.StartDate).ValidPickupDate();
        RuleFor(x => x.EndDate).ValidReturnDate(x => x.StartDate);
        RuleFor(x => x.PickupLocation).ValidPickupLocation();
    }
}