using CarRental.Application.Abstractions;
using CarRental.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);

    public async Task<Result<Success>> TrySaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);

            return Result.Success;
        }
        catch (DbUpdateException exception) when (DatabaseConflicts.ErrorFor(exception) is { } conflict)
        {
            return conflict;
        }
    }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfTransaction(await context.Database.BeginTransactionAsync(cancellationToken));
}
