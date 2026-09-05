using CarRental.Domain.Enums;

namespace CarRental.Application.Abstractions;

public interface IEmailOutbox
{
    void Enqueue(OutboxEmailKind kind, string recipient, string firstName, string link);
}
