using CarRental.Application.Contracts.Auth;
using CarRental.Domain.Common;

namespace CarRental.Application.Abstractions;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task<Result<Success>> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<Success>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task<Result<Success>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
