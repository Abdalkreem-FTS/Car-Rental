namespace CarRental.Application.Abstractions;

public interface IEmailSender
{
    Task SendPasswordResetAsync(string email, string firstName, string resetLink, CancellationToken cancellationToken = default);

    Task SendEmailConfirmationAsync(string email, string firstName, string confirmLink, CancellationToken cancellationToken = default);
}
