namespace CarRental.Domain.Rules;

public static class RenterRules
{
    public const int MinimumAge = 18;

    public static int AgeOn(DateOnly birthDate, DateOnly today)
    {
        var age = today.Year - birthDate.Year;

        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    public static bool IsOldEnough(DateOnly? birthDate, DateOnly today) =>
        birthDate is { } date && AgeOn(date, today) >= MinimumAge;
}
