using CarRental.Application.Abstractions;
using CarRental.Domain.Common;
using CarRental.Infrastructure.Persistence.Conflicts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarRental.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext context, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);

    public async Task<Result<Success>> TrySaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);

            return Result.Success;
        }
        catch (DbUpdateException exception) when (DatabaseConflicts.ConflictFor(exception) is { } conflict)
        {
            return logger.Report(conflict, exception);
        }
    }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfTransaction(await context.Database.BeginTransactionAsync(cancellationToken));
}
