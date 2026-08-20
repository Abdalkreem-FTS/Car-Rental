using CarRental.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CarRental.Infrastructure.Email;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value.IsConfigured
        ? options.Value
        : throw new InvalidOperationException($"{SmtpOptions.SectionName}:Host must be set to send mail over SMTP.");

    public async Task SendPasswordResetAsync(
        string email,
        string firstName,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage
        {
            Subject = PasswordResetEmail.Subject,
            Body = new BodyBuilder
            {
                HtmlBody = PasswordResetEmail.Html(firstName, resetLink, TimeSpan.FromHours(1)),
                TextBody = PasswordResetEmail.Text(firstName, resetLink, TimeSpan.FromHours(1)),
            }.ToMessageBody(),
        };

        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(firstName, email));

        try
        {
            using var client = new SmtpClient();
            client.Timeout = (int)TimeSpan.FromSeconds(_options.TimeoutSeconds).TotalMilliseconds;

            await client.ConnectAsync(_options.Host!, _options.Port, SocketOptions(), cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            logger.LogInformation("Sent a password reset email via {Host}", _options.Host);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Could not send a password reset email via {Host}", _options.Host);
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
