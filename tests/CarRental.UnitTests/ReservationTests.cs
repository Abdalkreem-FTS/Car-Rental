using CarRental.Domain.Entities;
using Shouldly;

namespace CarRental.UnitTests;

public sealed class ReservationTests
{
    [Theory]
    [InlineData("2026-09-01", "2026-09-01", 1)]
    [InlineData("2026-09-01", "2026-09-02", 2)]
    [InlineData("2026-09-01", "2026-09-05", 5)]
    [InlineData("2026-08-30", "2026-09-02", 4)]
    [InlineData("2026-12-28", "2027-01-03", 7)]
    [InlineData("2028-02-27", "2028-03-01", 4)]
    public void TotalDays_ForADateRange_CountsBothEnds(string start, string end, int expectedDays)
    {
        var reservation = NewReservation(DateOnly.Parse(start), DateOnly.Parse(end));

        reservation.TotalDays.ShouldBe(expectedDays);
    }


    private static Reservation NewReservation(DateOnly start, DateOnly end) => new()
    {
        UserId = Guid.NewGuid(),
        CarId = Guid.NewGuid(),
        StartDate = start,
        EndDate = end,
        TotalPrice = 0m,
    };
}

public sealed class TokenLifetimeTests
{
    [Fact]
    public void IsActive_ForARefreshToken_IsTrueOnlyBeforeExpiryAndUntilRevoked()
    {
        var now = new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

        var live = NewRefreshToken(now.AddDays(1));
        live.IsActive(now).ShouldBeTrue();

        var expired = NewRefreshToken(now.AddSeconds(-1));
        expired.IsActive(now).ShouldBeFalse();

        var revoked = NewRefreshToken(now.AddDays(1));
        revoked.RevokedAtUtc = now;
        revoked.IsActive(now).ShouldBeFalse();
    }


    private static RefreshToken NewRefreshToken(DateTimeOffset expiresAtUtc) => new()
    {
        UserId = Guid.NewGuid(),
        TokenHash = RefreshToken.HashOf("token"),
        ExpiresAtUtc = expiresAtUtc,
    };

}
