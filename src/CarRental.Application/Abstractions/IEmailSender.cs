namespace CarRental.Application.Abstractions;

public interface IEmailSender
{
    Task SendPasswordResetAsync(string email, string firstName, string resetLink, CancellationToken cancellationToken = default);
}
