using CarRental.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CarRental.Infrastructure.Email;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendPasswordResetAsync(string email, string firstName, string resetLink, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "[DEV EMAIL] Password reset for {Email} ({FirstName}). Open this link to reset: {ResetLink}",
            email,
            firstName,
            resetLink);

        return Task.CompletedTask;
    }
}
