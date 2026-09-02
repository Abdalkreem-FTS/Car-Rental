using System.ComponentModel.DataAnnotations;

namespace CarRental.Infrastructure.Email.Delivery;

public sealed class EmailDeliveryOptions
{
    public const string SectionName = "EmailDelivery";

    public bool BackgroundDelivery { get; set; } = true;

    [Range(1, 3600)]
    public int PollSeconds { get; set; } = 15;

    [Range(1, 20)]
    public int MaxAttempts { get; set; } = 5;

    [Range(1, 3600)]
    public int FirstRetrySeconds { get; set; } = 30;

    [Range(1, 200)]
    public int BatchSize { get; set; } = 20;
}
