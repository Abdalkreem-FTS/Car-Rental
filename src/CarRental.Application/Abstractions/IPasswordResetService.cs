using CarRental.Application.Dtos.Auth;
using CarRental.Domain.Common;

namespace CarRental.Application.Abstractions;

public interface IPasswordResetService
{
    Task<Result<Success>> ForgotPasswordAsync(ForgotPasswordDto request, CancellationToken cancellationToken = default);

    Task<Result<Success>> ResetPasswordAsync(ResetPasswordDto request, CancellationToken cancellationToken = default);
}
