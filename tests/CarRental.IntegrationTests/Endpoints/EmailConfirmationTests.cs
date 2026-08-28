using System.Net;
using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class EmailConfirmationTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Register_WhenAnAccountIsCreated_SendsAConfirmationLink()
    {
        var registration = TestData.Registration();

        await SignUpAsync(registration, confirmEmail: false);

        var sent = Factory.Emails.Sent.ShouldHaveSingleItem();

        sent.Kind.ShouldBe(EmailKind.EmailConfirmation);
        sent.Email.ShouldBe(registration.Email);
    }

    [Fact]
    public async Task Register_BeforeConfirming_LeavesTheAccountUnconfirmed()
    {
        var registration = TestData.Registration();

        await SignUpAsync(registration, confirmEmail: false);

        var confirmed = await Factory.WithDbAsync(db =>
            db.Users.Where(user => user.Email == registration.Email).Select(user => user.EmailConfirmed).SingleAsync());

        confirmed.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateReservation_BeforeConfirmingTheAddress_IsRefused()
    {
        await SignUpAsync(confirmEmail: false);
        var car = await FindCarAsync("Corolla");

        var response = await Api.Reservations.CreateAsync(TestData.Booking(car.Id, 5, 7));

        response.ShouldBeForbidden(UserErrors.EmailNotConfirmed);
    }

    [Fact]
    public async Task CreateReservation_OnceTheAddressIsConfirmed_Succeeds()
    {
        var registration = TestData.Registration();
        await SignUpAsync(registration, confirmEmail: false);

        var car = await FindCarAsync("Corolla");
        (await Api.Reservations.CreateAsync(TestData.Booking(car.Id, 5, 7))).ShouldBeForbidden(UserErrors.EmailNotConfirmed);

        await ConfirmEmailAsync(registration.Email);

        (await Api.Reservations.CreateAsync(TestData.Booking(car.Id, 5, 7))).ShouldBeCreated();
    }

    [Fact]
    public async Task Browsing_BeforeConfirmingTheAddress_IsStillAllowed()
    {
        await SignUpAsync(confirmEmail: false);

        (await Api.Cars.SearchAsync()).ShouldBeOk();
        (await Api.Profile.GetAsync()).ShouldBeOk();
        (await Api.Reservations.ListAsync()).ShouldBeOk();
    }

    [Fact]
    public async Task ConfirmEmail_WithAMangledToken_IsRefused()
    {
        var registration = TestData.Registration();
        await SignUpAsync(registration, confirmEmail: false);

        var response = await Api.Auth.ConfirmEmailAsync(registration.Email, "not-a-real-token");

        response.ShouldFailValidation("token");
    }

    [Fact]
    public async Task ConfirmEmail_WithAnotherAccountsToken_IsRefused()
    {
        var mine = TestData.Registration();
        await SignUpAsync(mine, confirmEmail: false);
        var myToken = Factory.Emails.ConfirmationTokenFor(mine.Email);

        var theirs = TestData.Registration();
        await SignUpAsync(theirs, confirmEmail: false);

        (await Api.Auth.ConfirmEmailAsync(theirs.Email, myToken)).ShouldFailValidation("token");
    }

    [Fact]
    public async Task ConfirmEmail_Twice_IsAccepted()
    {
        var registration = TestData.Registration();
        await SignUpAsync(registration, confirmEmail: false);

        await ConfirmEmailAsync(registration.Email);

        var token = Factory.Emails.ConfirmationTokenFor(registration.Email);

        (await Api.Auth.ConfirmEmailAsync(registration.Email, token)).ShouldBeNoContent();
    }

    [Fact]
    public async Task ResendConfirmation_ForAnUnconfirmedAccount_SendsAnotherLinkThatWorks()
    {
        var registration = TestData.Registration();
        await SignUpAsync(registration, confirmEmail: false);

        (await Api.Auth.ResendConfirmationAsync(registration.Email)).ShouldBeAccepted();

        Factory.Emails.Sent.Count(sent => sent.Kind == EmailKind.EmailConfirmation).ShouldBe(2);

        await ConfirmEmailAsync(registration.Email);
    }

    [Fact]
    public async Task ResendConfirmation_ForAnUnknownOrConfirmedAddress_AnswersIdentically()
    {
        var registration = TestData.Registration();
        await SignUpAsync(registration);

        var confirmed = await Api.Auth.ResendConfirmationAsync(registration.Email);
        var unknown = await Api.Auth.ResendConfirmationAsync("nobody@example.com");

        confirmed.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        unknown.StatusCode.ShouldBe(confirmed.StatusCode, "the answer must not reveal whether an account exists");
    }
}
