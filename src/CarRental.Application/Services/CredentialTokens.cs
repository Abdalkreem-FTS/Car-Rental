using System.Buffers.Text;
using System.Text;

namespace CarRental.Application.Services;

internal static class CredentialTokens
{
    internal static string Encode(string token) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(token));

    internal static bool TryDecode(string encoded, out string token)
    {
        try
        {
            token = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(encoded));

            return true;
        }
        catch (FormatException)
        {
            token = string.Empty;

            return false;
        }
    }
}
