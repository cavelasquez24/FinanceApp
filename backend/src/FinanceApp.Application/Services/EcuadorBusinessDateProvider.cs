using FinanceApp.Application.Interfaces;

namespace FinanceApp.Application.Services;

/// <summary>
/// Política temporal actual de FinFlow. Las fechas de negocio se muestran y
/// calculan en America/Guayaquil, independientemente de la zona del host.
/// Los timestamps técnicos continúan almacenándose en UTC.
/// </summary>
public sealed class EcuadorBusinessDateProvider : IBusinessDateProvider
{
    private static readonly TimeZoneInfo BusinessTimeZone = ResolveTimeZone();
    private readonly TimeProvider _timeProvider;

    public EcuadorBusinessDateProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DateOnly Today => GetDate(_timeProvider.GetUtcNow());

    public DateOnly GetDate(DateTimeOffset instant) => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(instant, BusinessTimeZone).DateTime);

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        }
    }

}
