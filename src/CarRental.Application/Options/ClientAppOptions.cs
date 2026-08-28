namespace CarRental.Application.Options;

public sealed class ClientAppOptions
{
    public const string SectionName = "ClientApp";

    public string BaseUrl { get; set; } = "http://localhost:5000";

    public string ResetPasswordPath { get; set; } = "/reset-password.html";

    public string ConfirmEmailPath { get; set; } = "/confirm-email.html";
}
