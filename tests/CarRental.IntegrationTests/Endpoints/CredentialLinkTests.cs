using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class CredentialLinkTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ResetLink_WhenSent_KeepsTheTokenOutOfTheQueryString()
    {
        var registration = TestData.Registration();
        var auth = await SignUpAsync(registration);
        SignOut();

        (await Api.Auth.ForgotPasswordAsync(auth.User.Email)).ShouldBeAccepted();

        AssertCredentialIsInTheFragment(Factory.Emails.LinkFor(auth.User.Email));
    }

    [Fact]
    public async Task ConfirmationLink_WhenSent_KeepsTheTokenOutOfTheQueryString()
    {
        var registration = TestData.Registration();

        await SignUpAsync(registration, confirmEmail: false);

        var sent = Factory.Emails.Sent.Single(email => email.Kind == EmailKind.EmailConfirmation);

        AssertCredentialIsInTheFragment(sent.Link);
    }

    [Fact]
    public async Task ResetLink_WhateverThePathSeparators_IsAWellFormedAbsoluteUrl()
    {
        var auth = await SignUpAsync();
        SignOut();

        (await Api.Auth.ForgotPasswordAsync(auth.User.Email)).ShouldBeAccepted();

        var link = new Uri(Factory.Emails.LinkFor(auth.User.Email));

        link.IsAbsoluteUri.ShouldBeTrue();
        link.Host.ShouldBe("rentals.example.test");
        link.AbsolutePath.ShouldBe("/reset-password.html", "no doubled or missing separators");
    }

    private static void AssertCredentialIsInTheFragment(string link)
    {
        var uri = new Uri(link);

        uri.Query.ShouldBeEmpty("a query string reaches server logs, proxies and the Referer header");
        uri.Fragment.ShouldContain("token=");
        link.ShouldNotContain("?", Case.Sensitive);
    }
}
