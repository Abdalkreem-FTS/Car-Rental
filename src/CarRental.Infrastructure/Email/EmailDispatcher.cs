using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarRental.Infrastructure.Email;

public sealed class EmailDispatcher(
    AppDbContext context,
    IEmailSender sender,
    IOptions<EmailDeliveryOptions> options,
    ILogger<EmailDispatcher> logger)
{
    private readonly EmailDeliveryOptions _options = options.Value;

    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken = default)
    {
        var delivered = 0;

        foreach (var id in await DueIdsAsync(cancellationToken))
        {
            if (await ClaimAsync(id, cancellationToken) is not { } claimed)
            {
                continue;
            }

            if (await TrySendAsync(claimed, cancellationToken))
            {
                delivered++;
            }
        }

        return delivered;
    }

    private Task<List<Guid>> DueIdsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        return context.OutboxEmails
            .Where(email => email.SentAtUtc == null && email.AbandonedAtUtc == null && email.NextAttemptAtUtc <= now)
            .OrderBy(email => email.NextAttemptAtUtc)
            .Take(_options.BatchSize)
            .Select(email => email.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<OutboxEmail?> ClaimAsync(Guid id, CancellationToken cancellationToken)
    {
        var email = await context.OutboxEmails.AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == id, cancellationToken);

        if (email is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var seen = email.Attempts;
        var attempts = seen + 1;
        var nextAttempt = now + BackoffFor(attempts);

        var claimed = await context.OutboxEmails
            .Where(row =>
                row.Id == id &&
                row.Attempts == seen &&
                row.SentAtUtc == null &&
                row.AbandonedAtUtc == null &&
                row.NextAttemptAtUtc <= now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(row => row.Attempts, attempts)
                    .SetProperty(row => row.NextAttemptAtUtc, nextAttempt),
                cancellationToken);

        if (claimed != 1)
        {
            return null;
        }

        email.Attempts = attempts;

        return email;
    }

    private async Task<bool> TrySendAsync(OutboxEmail email, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(email, cancellationToken);

            var sentAt = DateTimeOffset.UtcNow;

            await context.OutboxEmails
                .Where(row => row.Id == email.Id)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(row => row.SentAtUtc, sentAt)
                        .SetProperty(row => row.LastError, (string?)null),
                    cancellationToken);

            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var abandoned = email.Attempts >= _options.MaxAttempts;
            var abandonedAt = abandoned ? DateTimeOffset.UtcNow : (DateTimeOffset?)null;
            var lastError = Describe(exception);

            logger.Log(
                abandoned ? LogLevel.Error : LogLevel.Warning,
                exception,
                "Could not deliver {Kind} to {Recipient} on attempt {Attempt} of {MaxAttempts}",
                email.Kind,
                email.Recipient,
                email.Attempts,
                _options.MaxAttempts);

            await context.OutboxEmails
                .Where(row => row.Id == email.Id)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(row => row.LastError, lastError)
                        .SetProperty(row => row.AbandonedAtUtc, abandonedAt),
                    cancellationToken);

            return false;
        }
    }

    private Task SendAsync(OutboxEmail email, CancellationToken cancellationToken) => email.Kind switch
    {
        OutboxEmailKind.PasswordReset =>
            sender.SendPasswordResetAsync(email.Recipient, email.FirstName, email.Link, cancellationToken),
        OutboxEmailKind.EmailConfirmation =>
            sender.SendEmailConfirmationAsync(email.Recipient, email.FirstName, email.Link, cancellationToken),
        _ => throw new NotSupportedException($"No sender for {email.Kind}."),
    };

    private TimeSpan BackoffFor(int attempts) =>
        TimeSpan.FromSeconds(_options.FirstRetrySeconds * Math.Pow(2, Math.Min(attempts - 1, 10)));

    private static string Describe(Exception exception) =>
        exception.Message.Length <= 1000 ? exception.Message : exception.Message[..1000];
}
