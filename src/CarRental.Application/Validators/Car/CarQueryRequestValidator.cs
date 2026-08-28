using CarRental.Application.Common;
using CarRental.Application.Contracts.Cars;
using FluentValidation;

namespace CarRental.Application.Validators.Car;

public sealed class CarQueryRequestValidator : AbstractValidator<CarQueryRequest>
{
    public CarQueryRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(Paging.FirstPage)
            .WithMessage($"Page must be {Paging.FirstPage} or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(Paging.MinPageSize, Paging.MaxPageSize)
            .WithMessage($"Page size must be between {Paging.MinPageSize} and {Paging.MaxPageSize}.");

        RuleFor(x => x.ReturnDate)
            .GreaterThanOrEqualTo(x => x.PickupDate!.Value)
            .When(x => x.PickupDate.HasValue && x.ReturnDate.HasValue)
            .WithMessage("The return date must be on or after the pickup date.");
    }
}
