using CarRental.Application.Dtos.Profile;
using CarRental.Domain.Common;

namespace CarRental.Application.Abstractions;

public interface IProfileService
{
    Task<Result<ProfileDto>> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<ProfileDto>> UpdateAsync(Guid userId, UpdateProfileDto request, CancellationToken cancellationToken = default);

    Task<Result<Updated>> ChangePasswordAsync(Guid userId, ChangePasswordDto request, CancellationToken cancellationToken = default);
}
