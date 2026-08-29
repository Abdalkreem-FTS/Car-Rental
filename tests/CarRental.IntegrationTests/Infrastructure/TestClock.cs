namespace CarRental.IntegrationTests.Infrastructure;

public sealed class TestClock : TimeProvider
{
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);

    public void ResetToRealTime() => _utcNow = DateTimeOffset.UtcNow;
}
