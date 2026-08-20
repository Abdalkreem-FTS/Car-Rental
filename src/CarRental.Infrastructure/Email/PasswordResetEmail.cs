using System.Net;

namespace CarRental.Infrastructure.Email;

internal static class PasswordResetEmail
{
    internal const string Subject = "Reset your Car Rental password";

    internal static string Html(string firstName, string resetLink, TimeSpan validFor)
    {
        var name = WebUtility.HtmlEncode(firstName);
        var href = WebUtility.HtmlEncode(resetLink);

        return $"""
            <!doctype html>
            <html lang="en">
            <body style="margin:0;padding:24px;background:#f5f6f8;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:#10131a;">
              <table role="presentation" cellpadding="0" cellspacing="0" style="max-width:520px;margin:0 auto;background:#ffffff;border:1px solid #e2e5ea;border-radius:16px;">
                <tr><td style="padding:28px 28px 0;">
                  <p style="margin:0 0 20px;font-size:18px;font-weight:600;">Car Rental</p>
                  <h1 style="margin:0 0 12px;font-size:22px;line-height:1.3;">Reset your password</h1>
                  <p style="margin:0 0 8px;font-size:15px;line-height:1.55;">Hi {name},</p>
                  <p style="margin:0 0 20px;font-size:15px;line-height:1.55;color:#5c6472;">
                    We received a request to reset the password on your Car Rental account.
                    This link works once and expires in {Describe(validFor)}.
                  </p>
                </td></tr>
                <tr><td style="padding:0 28px 20px;">
                  <a href="{href}" style="display:inline-block;background:#2f5bd7;color:#ffffff;text-decoration:none;font-size:15px;font-weight:600;padding:12px 22px;border-radius:10px;">Choose a new password</a>
                </td></tr>
                <tr><td style="padding:0 28px 24px;">
                  <p style="margin:0 0 6px;font-size:13px;color:#8a92a1;">Or paste this into your browser:</p>
                  <p style="margin:0 0 20px;font-size:12.5px;line-height:1.5;word-break:break-all;color:#2f5bd7;">{href}</p>
                  <p style="margin:0;padding-top:16px;border-top:1px solid #e2e5ea;font-size:13px;line-height:1.55;color:#8a92a1;">
                    If you did not ask for this, you can ignore this email — your password will not change.
                  </p>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    internal static string Text(string firstName, string resetLink, TimeSpan validFor) =>
        $"""
        Hi {firstName},

        We received a request to reset the password on your Car Rental account.
        Open this link to choose a new one. It works once and expires in {Describe(validFor)}.

        {resetLink}

        If you did not ask for this, you can ignore this email — your password will not change.

        — Car Rental
        """;

    private static string Describe(TimeSpan validFor) => validFor.TotalHours >= 1
        ? $"{validFor.TotalHours:0.#} hour{(validFor.TotalHours >= 2 ? "s" : string.Empty)}"
        : $"{validFor.TotalMinutes:0} minutes";
}
