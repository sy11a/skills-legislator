namespace Legislator.TestSupport;

/// <summary>A clock that never moves. The audit report stamps the day it was produced, so a test that wants to compare the whole report needs the date to be a value it chose.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
