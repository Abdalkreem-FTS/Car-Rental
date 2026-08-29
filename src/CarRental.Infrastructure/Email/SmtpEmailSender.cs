using CarRental.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CarRental.Infrastructure.Email;

public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    IOptions<DataProtectionTokenProviderOptions> tokenOptions,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value.IsConfigured
        ? options.Value
        : throw new InvalidOperationException($"{SmtpOptions.SectionName}:Host must be set to send mail over SMTP.");

    private readonly TimeSpan _linkLifetime = tokenOptions.Value.TokenLifespan;

    public Task SendPasswordResetAsync(
        string email,
        string firstName,
        string resetLink,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            email,
            firstName,
            PasswordResetEmail.Subject,
            PasswordResetEmail.Html(firstName, resetLink, _linkLifetime),
            PasswordResetEmail.Text(firstName, resetLink, _linkLifetime),
            cancellationToken);

    public Task SendEmailConfirmationAsync(
        string email,
        string firstName,
        string confirmLink,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            email,
            firstName,
            EmailConfirmationEmail.Subject,
            EmailConfirmationEmail.Html(firstName, confirmLink),
            EmailConfirmationEmail.Text(firstName, confirmLink),
            cancellationToken);

    private async Task SendAsync(
        string email,
        string firstName,
        string subject,
        string html,
        string text,
        CancellationToken cancellationToken)
    {
        var message = new MimeMessage
        {
            Subject = subject,
            Body = new BodyBuilder { HtmlBody = html, TextBody = text }.ToMessageBody(),
        };

        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(firstName, email));

        using var client = new SmtpClient();

        try
        {
            client.Timeout = (int)TimeSpan.FromSeconds(_options.TimeoutSeconds).TotalMilliseconds;

            await client.ConnectAsync(_options.Host!, _options.Port, SocketOptions(), cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);

            logger.LogInformation("Sent {Subject} via {Host}", subject, _options.Host);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(quit: true, CancellationToken.None);
            }
        }
    }

    private SecureSocketOptions SocketOptions() => _options.Security switch
    {
        SmtpSecurity.None => SecureSocketOptions.None,
        SmtpSecurity.StartTls => SecureSocketOptions.StartTls,
        SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
        _ => SecureSocketOptions.Auto,
    };
}
