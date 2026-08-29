using CarRental.Application.Common;
using CarRental.Application.Contracts.Reservations;
using FluentValidation;

namespace CarRental.Application.Validators.Reservation;

public sealed class ReservationQueryValidator : AbstractValidator<ReservationQuery>
{
    public ReservationQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(Paging.FirstPage)
            .WithMessage($"Page must be {Paging.FirstPage} or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(Paging.MinPageSize, Paging.MaxPageSize)
            .WithMessage($"Page size must be between {Paging.MinPageSize} and {Paging.MaxPageSize}.");
    }
}
