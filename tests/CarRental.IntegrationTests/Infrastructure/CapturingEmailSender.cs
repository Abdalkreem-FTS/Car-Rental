using System.Collections.Concurrent;
using System.Web;
using CarRental.Application.Abstractions;

namespace CarRental.IntegrationTests.Infrastructure;

public enum EmailKind
{
    PasswordReset,
    EmailConfirmation,
}

public sealed record SentEmail(EmailKind Kind, string Email, string FirstName, string Link)
{
    public string ResetLink => Link;
}

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> Sent => [.. _sent];

    public IReadOnlyCollection<SentEmail> Resets => [.. _sent.Where(sent => sent.Kind == EmailKind.PasswordReset)];

    public Task SendPasswordResetAsync(string email, string firstName, string resetLink, CancellationToken cancellationToken = default)
    {
        if (_failure is { } reason)
        {
            throw new InvalidOperationException(reason);
        }

        _sent.Enqueue(new SentEmail(EmailKind.PasswordReset, email, firstName, resetLink));

        return Task.CompletedTask;
    }

    public Task SendEmailConfirmationAsync(string email, string firstName, string confirmLink, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(EmailKind.EmailConfirmation, email, firstName, confirmLink));

        return Task.CompletedTask;
    }

    public void Clear()
    {
        _sent.Clear();
        _failure = null;
    }

    private string? _failure;

    public void FailWith(string reason) => _failure = reason;

    public void Recover() => _failure = null;

    private SentEmail LastFor(string email, EmailKind kind) =>
        _sent.LastOrDefault(sent => sent.Kind == kind && string.Equals(sent.Email, email, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"No {kind} email was sent to '{email}'.");

    public string LinkFor(string email) => LastFor(email, EmailKind.PasswordReset).Link;

    public string TokenFor(string email) => TokenIn(LastFor(email, EmailKind.PasswordReset).Link, email);

    public string ConfirmationTokenFor(string email) => TokenIn(LastFor(email, EmailKind.EmailConfirmation).Link, email);

    private static string TokenIn(string link, string email)
    {
        var query = HttpUtility.ParseQueryString(new Uri(link).Fragment.TrimStart('#'));

        var linkedEmail = query["email"] ?? throw new InvalidOperationException($"No 'email' in link: {link}");
        var token = query["token"] ?? throw new InvalidOperationException($"No 'token' in link: {link}");

        return string.Equals(linkedEmail, email, StringComparison.OrdinalIgnoreCase)
            ? token
            : throw new InvalidOperationException($"Link was addressed to '{linkedEmail}', not '{email}'.");
    }
}
