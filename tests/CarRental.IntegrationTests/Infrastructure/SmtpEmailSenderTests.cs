using CarRental.Infrastructure.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace CarRental.IntegrationTests.Infrastructure;

public sealed class SmtpEmailSenderTests
{
    [Fact]
    public async Task SendPasswordReset_WhenTheServerCannotBeReached_ThrowsSoTheOutboxCanRetry()
    {
        var sender = SenderOn(UnreachablePort, TimeSpan.FromHours(1));

        await Should.ThrowAsync<Exception>(
            () => sender.SendPasswordResetAsync("someone@example.test", "Someone", "https://example.test/reset"));
    }

    [Theory]
    [InlineData(30, "30 minutes")]
    [InlineData(120, "2 hours")]
    public async Task SendPasswordReset_TellsTheReaderTheLifetimeIdentityActuallyEnforces(int minutes, string expected)
    {
        await using var stub = new SmtpStub();

        var sender = SenderOn(stub.Port, TimeSpan.FromMinutes(minutes));

        await sender.SendPasswordResetAsync("someone@example.test", "Someone", "https://example.test/reset");

        var message = await stub.MessageAsync().WaitAsync(TimeSpan.FromSeconds(10));

        message.ShouldContain(expected, Case.Insensitive,
            "the expiry in the email has to come from the lifespan Identity enforces, not a second copy of it");
    }

    private const int UnreachablePort = 9;

    private static SmtpEmailSender SenderOn(int port, TimeSpan tokenLifespan) => new(
        Options.Create(new SmtpOptions
        {
            Host = "127.0.0.1",
            Port = port,
            FromAddress = "no-reply@example.test",
            FromName = "Car Rental",
            TimeoutSeconds = 5,
            Security = SmtpSecurity.None,
        }),
        Options.Create(new DataProtectionTokenProviderOptions { TokenLifespan = tokenLifespan }),
        NullLogger<SmtpEmailSender>.Instance);
}
