using CarRental.Application.Contracts.Auth;
using CarRental.Domain.Common;

namespace CarRental.Application.Abstractions;

public interface IRegistrationService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<Result<Success>> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default);

    Task<Result<Success>> ResendConfirmationAsync(ResendConfirmationRequest request, CancellationToken cancellationToken = default);
}
