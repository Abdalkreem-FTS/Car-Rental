namespace CarRental.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default);
}
