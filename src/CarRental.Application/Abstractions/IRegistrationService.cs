using CarRental.Application.Dtos.Auth;
using CarRental.Domain.Common;

namespace CarRental.Application.Abstractions;

public interface IRegistrationService
{
    Task<Result<AuthDto>> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default);

    Task<Result<Success>> ConfirmEmailAsync(ConfirmEmailDto request, CancellationToken cancellationToken = default);

    Task<Result<Success>> ResendConfirmationAsync(ResendConfirmationDto request, CancellationToken cancellationToken = default);
}
