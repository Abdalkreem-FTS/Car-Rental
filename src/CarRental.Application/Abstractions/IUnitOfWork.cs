using CarRental.Domain.Common;

namespace CarRental.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves, reporting a conflict the database refused as an error rather than throwing.
    /// </summary>
    Task<Result<Success>> TrySaveChangesAsync(CancellationToken cancellationToken = default);
}
