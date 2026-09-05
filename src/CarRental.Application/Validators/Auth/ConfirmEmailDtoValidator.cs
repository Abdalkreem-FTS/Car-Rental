using CarRental.Application.Dtos.Auth;
using FluentValidation;

namespace CarRental.Application.Validators.Auth;

public sealed class ConfirmEmailDtoValidator : AbstractValidator<ConfirmEmailDto>
{
    public ConfirmEmailDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("Confirmation token is required.");
    }
}

public sealed class ResendConfirmationDtoValidator : AbstractValidator<ResendConfirmationDto>
{
    public ResendConfirmationDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.");
    }
}
