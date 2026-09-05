using CarRental.IntegrationTests.Support;
using CarRental.IntegrationTests.Support.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace CarRental.IntegrationTests.Contracts;

public sealed class PasswordPolicyTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Registration_RejectsExactlyWhatIdentityWouldReject()
    {
        var policy = Factory.Services.GetRequiredService<IOptions<IdentityOptions>>().Value.Password;

        var tooShort = new string('A', policy.RequiredLength - 1) + "a1!";

        var response = await Api.Auth.RegisterAsync(
            TestData.Registration() with { Password = tooShort[..(policy.RequiredLength - 1)], ConfirmPassword = tooShort[..(policy.RequiredLength - 1)] });

        var problem = response.ShouldFailValidationOn("password");

        problem.Errors!["password"].ShouldContain(
            message => message.Contains(policy.RequiredLength.ToString()),
            "the message must quote the configured length, not a copy of it");
    }
}
