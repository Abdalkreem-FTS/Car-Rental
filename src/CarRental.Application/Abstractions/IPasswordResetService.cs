using CarRental.Application.Contracts.Auth;
using CarRental.Domain.Common;

namespace CarRental.Application.Abstractions;

public interface IPasswordResetService
{
    Task<Result<Success>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task<Result<Success>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
