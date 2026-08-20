using System.Collections.Concurrent;
using System.Web;
using CarRental.Application.Abstractions;

namespace CarRental.IntegrationTests.Infrastructure;

public sealed record SentEmail(string Email, string FirstName, string ResetLink);

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> Sent => [.. _sent];

    public Task SendPasswordResetAsync(string email, string firstName, string resetLink, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(email, firstName, resetLink));

        return Task.CompletedTask;
    }

    public void Clear() => _sent.Clear();

    private SentEmail LastFor(string email) =>
        _sent.LastOrDefault(sent => string.Equals(sent.Email, email, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"No password reset email was sent to '{email}'.");
    
    public string LinkFor(string email) => LastFor(email).ResetLink;

    public string TokenFor(string email)
    {
        var link = LastFor(email).ResetLink;
        var query = HttpUtility.ParseQueryString(new Uri(link).Query);

        var linkedEmail = query["email"] ?? throw new InvalidOperationException($"No 'email' in reset link: {link}");
        var token = query["token"] ?? throw new InvalidOperationException($"No 'token' in reset link: {link}");

        return !string.Equals(linkedEmail, email, StringComparison.OrdinalIgnoreCase) ? throw new InvalidOperationException($"Reset link was addressed to '{linkedEmail}', not '{email}'.") : token;
    }
}
