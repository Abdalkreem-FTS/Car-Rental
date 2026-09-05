using CarRental.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace CarRental.Infrastructure.Persistence;

internal sealed class EfTransaction(IDbContextTransaction transaction) : ITransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
