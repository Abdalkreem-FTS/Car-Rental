using System.ComponentModel.DataAnnotations;

namespace CarRental.Application.Options;

public sealed class ClientAppOptions
{
    public const string SectionName = "ClientApp";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "http://localhost:5000";

    [Required]
    public string ResetPasswordPath { get; set; } = "/reset-password.html";

    [Required]
    public string ConfirmEmailPath { get; set; } = "/confirm-email.html";

    public Uri LinkTo(string path, string email, string token) => new UriBuilder(new Uri(new Uri(BaseUrl), path))
    {
        Fragment = $"email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}",
    }.Uri;
}
