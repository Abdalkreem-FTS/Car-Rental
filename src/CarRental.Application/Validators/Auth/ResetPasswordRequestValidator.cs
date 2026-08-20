using CarRental.Application.Contracts.Auth;
using FluentValidation;

namespace CarRental.Application.Validators.Auth;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("Enter a valid email address.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("This password reset link is incomplete. Please request a new one.");

        RuleFor(x => x.Password).ValidPassword();

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Please confirm your password.")
            .Equal(x => x.Password).WithMessage("The passwords do not match.");
    }
}
