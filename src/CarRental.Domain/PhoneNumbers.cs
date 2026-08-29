using System.Text;

namespace CarRental.Domain;

public static class PhoneNumbers
{
    public const int MinimumDigits = 7;

    public const int MaximumDigits = 15;

    public static bool TryNormalise(string? input, out string e164)
    {
        e164 = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();

        if (trimmed[0] != '+')
        {
            return false;
        }

        var digits = new StringBuilder(trimmed.Length);

        foreach (var character in trimmed[1..])
        {
            if (char.IsAsciiDigit(character))
            {
                digits.Append(character);
            }
            else if (character is not (' ' or '-' or '(' or ')' or '.'))
            {
                return false;
            }
        }

        if (digits.Length is < MinimumDigits or > MaximumDigits || digits[0] == '0')
        {
            return false;
        }

        e164 = $"+{digits}";

        return true;
    }

    public static string Normalise(string input) =>
        TryNormalise(input, out var e164) ? e164 : input.Trim();
}
