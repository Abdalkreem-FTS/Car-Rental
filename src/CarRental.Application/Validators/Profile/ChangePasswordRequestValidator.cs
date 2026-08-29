using CarRental.Application.Contracts.Profile;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CarRental.Application.Validators.Profile;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator(IOptions<IdentityOptions> identity)
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Your current password is required.");

        RuleFor(x => x.NewPassword)
            .ValidPassword(identity.Value.Password)
            .NotEqual(x => x.CurrentPassword).WithMessage("Your new password must be different from your current one.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Please confirm your new password.")
            .Equal(x => x.NewPassword).WithMessage("The passwords do not match.");
    }
}
