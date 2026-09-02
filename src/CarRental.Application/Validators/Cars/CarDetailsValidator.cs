using CarRental.Application.Dtos.Cars;
using FluentValidation;

namespace CarRental.Application.Validators.Cars;

public abstract class CarDetailsValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : ICarDetails
{
    protected CarDetailsValidator()
    {
        RuleFor(x => x.Make).ValidCarText("Make", 60);
        RuleFor(x => x.Model).ValidCarText("Model", 60);
        RuleFor(x => x.PlateNumber).ValidPlateNumber();
        RuleFor(x => x.Location).ValidCarText("Location", 120);
        RuleFor(x => x.Year).ValidCarYear();
        RuleFor(x => x.DailyRate).ValidDailyRate();
        RuleFor(x => x.Seats).ValidSeats();
        RuleFor(x => x.Category).IsInEnum().WithMessage("Select a valid category.");
        RuleFor(x => x.Transmission).IsInEnum().WithMessage("Select a valid transmission type.");
        RuleFor(x => x.Fuel).IsInEnum().WithMessage("Select a valid fuel type.");
        RuleFor(x => x.ImageUrl).MaximumLength(500).WithMessage("Image URL cannot exceed 500 characters.");
        RuleFor(x => x.Description).MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
    }
}
