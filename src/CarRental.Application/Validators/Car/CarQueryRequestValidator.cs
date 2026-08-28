using CarRental.Application.Contracts.Cars;
using FluentValidation;

namespace CarRental.Application.Validators.Car;

public sealed class CarQueryRequestValidator : AbstractValidator<CarQueryRequest>
{
    public CarQueryRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be 1 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Page size must be between 1 and 50.");

        RuleFor(x => x.ReturnDate)
            .GreaterThanOrEqualTo(x => x.PickupDate!.Value)
            .When(x => x.PickupDate.HasValue && x.ReturnDate.HasValue)
            .WithMessage("The return date must be on or after the pickup date.");
    }
}
