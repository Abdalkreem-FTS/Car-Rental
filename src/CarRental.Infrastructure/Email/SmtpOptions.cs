using System.ComponentModel.DataAnnotations;

namespace CarRental.Infrastructure.Email;

public enum SmtpSecurity
{
    None,
    StartTls,
    SslOnConnect,
    Auto
}

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";
    
    public string? Host { get; set; }

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public SmtpSecurity Security { get; set; } = SmtpSecurity.StartTls;

    public string? Username { get; set; }

    public string? Password { get; set; }

    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = "no-reply@carrental.local";

    public string FromName { get; set; } = "Car Rental";

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 15;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
