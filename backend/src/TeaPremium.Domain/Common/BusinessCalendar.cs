namespace TeaPremium.Domain.Common;

/// <summary>Business dates for the single vi-VN shop; persisted instants remain UTC.</summary>
public static class BusinessCalendar
{
    public static TimeSpan UtcOffset => TimeSpan.FromHours(7);

    public static DateOnly DateAt(DateTimeOffset instant) =>
        DateOnly.FromDateTime(instant.ToOffset(UtcOffset).DateTime);

    public static DateTimeOffset StartOfDayUtc(DateOnly date) =>
        new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), UtcOffset).ToUniversalTime();
}
