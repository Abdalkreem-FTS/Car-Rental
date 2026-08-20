using CarRental.Application.Contracts.Cars;
using FluentValidation;

namespace CarRental.Application.Validators.Car;

public sealed class CarSearchRequestValidator : AbstractValidator<CarSearchRequest>
{
    public CarSearchRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be 1 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Page size must be between 1 and 50.");

        RuleFor(x => x.MinSeats)
            .InclusiveBetween(1, 20).When(x => x.MinSeats.HasValue)
            .WithMessage("Minimum seats must be between 1 and 20.");

        RuleFor(x => x.MinDailyRate)
            .GreaterThanOrEqualTo(0).When(x => x.MinDailyRate.HasValue)
            .WithMessage("Minimum price cannot be negative.");

        RuleFor(x => x.MaxDailyRate)
            .GreaterThanOrEqualTo(0).When(x => x.MaxDailyRate.HasValue)
            .WithMessage("Maximum price cannot be negative.");

        RuleFor(x => x.MaxDailyRate)
            .GreaterThanOrEqualTo(x => x.MinDailyRate!.Value)
            .When(x => x.MinDailyRate.HasValue && x.MaxDailyRate.HasValue)
            .WithMessage("Maximum price must be greater than or equal to the minimum price.");

        RuleFor(x => x.ReturnDate)
            .GreaterThanOrEqualTo(x => x.PickupDate!.Value)
            .When(x => x.PickupDate.HasValue && x.ReturnDate.HasValue)
            .WithMessage("The return date must be on or after the pickup date.");
    }
}