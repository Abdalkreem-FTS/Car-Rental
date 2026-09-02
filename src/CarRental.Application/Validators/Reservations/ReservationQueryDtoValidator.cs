using CarRental.Application.Common;
using CarRental.Application.Dtos.Reservations;
using FluentValidation;

namespace CarRental.Application.Validators.Reservations;

public sealed class ReservationQueryDtoValidator : AbstractValidator<ReservationQueryDto>
{
    public ReservationQueryDtoValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(Paging.FirstPage)
            .WithMessage($"Page must be {Paging.FirstPage} or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(Paging.MinPageSize, Paging.MaxPageSize)
            .WithMessage($"Page size must be between {Paging.MinPageSize} and {Paging.MaxPageSize}.");
    }
}
