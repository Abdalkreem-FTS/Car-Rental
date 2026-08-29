using CarRental.Domain.Enums;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class EmailOutboxTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ForgotPassword_WhenAccepted_HasAlreadyDurablyQueuedTheEmail()
    {
        var auth = await SignUpAsync();
        SignOut();

        (await Api.Auth.ForgotPasswordAsync(auth.User.Email)).ShouldBeAccepted();

        var queued = await Factory.WithDbAsync(db => db.OutboxEmails
            .SingleAsync(email => email.Recipient == auth.User.Email && email.Kind == OutboxEmailKind.PasswordReset));

        queued.SentAtUtc.ShouldBeNull("202 means accepted for delivery, and delivery has not happened yet");
        queued.Attempts.ShouldBe(0);
    }

    [Fact]
    public async Task ForgotPassword_ForAnUnknownAddress_QueuesNothing()
    {
        (await Api.Auth.ForgotPasswordAsync("nobody@example.com")).ShouldBeAccepted();

        var queued = await Factory.WithDbAsync(db => db.OutboxEmails
            .CountAsync(email => email.Recipient == "nobody@example.com"));

        queued.ShouldBe(0);
    }

    [Fact]
    public async Task Delivery_WhenItRuns_MarksTheMessageSentAndDoesNotSendItTwice()
    {
        var auth = await SignUpAsync();
        SignOut();

        (await Api.Auth.ForgotPasswordAsync(auth.User.Email)).ShouldBeAccepted();

        (await Factory.DeliverQueuedEmailAsync()).ShouldBe(1);
        (await Factory.DeliverQueuedEmailAsync()).ShouldBe(0, "a sent message must not be sent again");

        var queued = await Factory.WithDbAsync(db => db.OutboxEmails
            .SingleAsync(email => email.Recipient == auth.User.Email && email.Kind == OutboxEmailKind.PasswordReset));

        queued.SentAtUtc.ShouldNotBeNull();
        queued.Attempts.ShouldBe(1);
        queued.LastError.ShouldBeNull();

        Factory.Emails.Resets.Count(sent => sent.Email == auth.User.Email).ShouldBe(1);
    }

    [Fact]
    public async Task Delivery_WhenTheTransportFails_RecordsTheReasonAndScheduleARetry()
    {
        var auth = await SignUpAsync();
        SignOut();

        (await Api.Auth.ForgotPasswordAsync(auth.User.Email)).ShouldBeAccepted();

        Factory.Emails.FailWith("the mail server is down");

        try
        {
            (await Factory.DeliverQueuedEmailAsync()).ShouldBe(0);
        }
        finally
        {
            Factory.Emails.Recover();
        }

        var queued = await Factory.WithDbAsync(db => db.OutboxEmails
            .SingleAsync(email => email.Recipient == auth.User.Email && email.Kind == OutboxEmailKind.PasswordReset));

        queued.SentAtUtc.ShouldBeNull();
        queued.Attempts.ShouldBe(1);
        queued.LastError.ShouldBe("the mail server is down", "a swallowed failure is why this was invisible before");
        queued.NextAttemptAtUtc.ShouldBeGreaterThan(DateTimeOffset.UtcNow, "the retry is scheduled, not immediate");
    }

    [Fact]
    public async Task Delivery_WhileARetryIsStillPending_LeavesTheMessageAlone()
    {
        var auth = await SignUpAsync();
        SignOut();

        (await Api.Auth.ForgotPasswordAsync(auth.User.Email)).ShouldBeAccepted();

        Factory.Emails.FailWith("down");

        try
        {
            await Factory.DeliverQueuedEmailAsync();
        }
        finally
        {
            Factory.Emails.Recover();
        }

        (await Factory.DeliverQueuedEmailAsync()).ShouldBe(0, "the backoff has not elapsed");
    }

    [Fact]
    public async Task Register_WhenAnAccountIsCreated_QueuesTheConfirmationRatherThanSendingItInline()
    {
        var registration = TestData.Registration();

        await SignUpAsync(registration, confirmEmail: false);

        var queued = await Factory.WithDbAsync(db => db.OutboxEmails
            .SingleAsync(email => email.Recipient == registration.Email));

        queued.Kind.ShouldBe(OutboxEmailKind.EmailConfirmation);
    }
}
