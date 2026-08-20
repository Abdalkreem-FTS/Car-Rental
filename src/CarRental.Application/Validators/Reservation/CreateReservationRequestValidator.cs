using CarRental.Application.Contracts.Reservations;
using FluentValidation;

namespace CarRental.Application.Validators.Reservation;

public sealed class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(x => x.CarId)
            .NotEmpty().WithMessage("Please choose a car to book.");

        RuleFor(x => x.StartDate).ValidPickupDate();
        RuleFor(x => x.EndDate).ValidReturnDate(x => x.StartDate);
        RuleFor(x => x.PickupLocation).ValidPickupLocation();
    }
}