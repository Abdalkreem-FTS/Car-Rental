using CarRental.Application.Contracts.Profile;
using CarRental.Domain.Common;

namespace CarRental.Application.Abstractions;

public interface IProfileService
{
    Task<Result<ProfileResponse>> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<ProfileResponse>> UpdateAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task<Result<Updated>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
