using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;

namespace CarRental.Infrastructure.Persistence.Stores;

public sealed class EmailOutbox(AppDbContext context) : IEmailOutbox
{
    public void Enqueue(OutboxEmailKind kind, string recipient, string firstName, string link) =>
        context.OutboxEmails.Add(new OutboxEmail
        {
            Kind = kind,
            Recipient = recipient,
            FirstName = firstName,
            Link = link,
        });
}
