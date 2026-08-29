using CarRental.Application.Common;
using CarRental.Application.Contracts.Cars;
using FluentValidation;

namespace CarRental.Application.Validators.Car;

public sealed class CarSearchRequestValidator : AbstractValidator<CarSearchRequest>
{
    public CarSearchRequestValidator()
    {
        RuleFor(x => x.Query).ValidSearchTerm();

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(Paging.FirstPage)
            .WithMessage($"Page must be {Paging.FirstPage} or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(Paging.MinPageSize, Paging.MaxPageSize)
            .WithMessage($"Page size must be between {Paging.MinPageSize} and {Paging.MaxPageSize}.");

        RuleFor(x => x.SortBy).ValidCarSortKey();

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

        RuleFor(x => x.PickupDate)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime))
            .When(x => x.PickupDate.HasValue)
            .WithMessage("The pickup date cannot be in the past.");

        RuleFor(x => x.ReturnDate)
            .NotNull()
            .When(x => x.PickupDate.HasValue)
            .WithMessage("Give a return date as well, or availability cannot be worked out.");

        RuleFor(x => x.PickupDate)
            .NotNull()
            .When(x => x.ReturnDate.HasValue)
            .WithMessage("Give a pickup date as well, or availability cannot be worked out.");
    }
}