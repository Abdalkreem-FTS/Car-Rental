using CarRental.Application.Dtos.Reservations;
using FluentValidation;

namespace CarRental.Application.Validators.Reservations;

public sealed class CreateReservationDtoValidator : AbstractValidator<CreateReservationDto>
{
    public CreateReservationDtoValidator()
    {
        RuleFor(x => x.CarId)
            .NotEmpty().WithMessage("Please choose a car to book.");

        RuleFor(x => x.StartDate).ValidPickupDate();
        RuleFor(x => x.EndDate).ValidReturnDate(x => x.StartDate);
        RuleFor(x => x.PickupLocation).ValidPickupLocation();
    }
}
