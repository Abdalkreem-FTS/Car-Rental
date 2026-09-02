using CarRental.Application.Dtos.Auth;
using FluentValidation;

namespace CarRental.Application.Validators.Auth;

public sealed class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("A refresh token is required.");
    }
}
