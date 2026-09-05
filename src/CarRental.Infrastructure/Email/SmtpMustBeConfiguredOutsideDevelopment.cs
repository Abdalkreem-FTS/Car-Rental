using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CarRental.Infrastructure.Email;

internal sealed class SmtpMustBeConfiguredOutsideDevelopment(IHostEnvironment environment) : IValidateOptions<SmtpOptions>
{
    public ValidateOptionsResult Validate(string? name, SmtpOptions options) =>
        options.IsConfigured || environment.IsDevelopment()
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"{SmtpOptions.SectionName}:Host must be set outside Development. Without it the only sender left "
                + "writes password reset links into the log.");
}
