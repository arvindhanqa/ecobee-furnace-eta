namespace EcobeeFurnaceEta.Blazor.Models;

/// <summary>
/// Runtime statistics for furnace operation.
/// </summary>
public class RuntimeStats
{
    /// <summary>
    /// Total heating runtime in minutes for last 24 hours.
    /// </summary>
    public int TotalHeatingMinutes24h { get; set; }

    /// <summary>
    /// Total cooling runtime in minutes for last 24 hours.
    /// </summary>
    public int TotalCoolingMinutes24h { get; set; }

    /// <summary>
    /// Current heating cycle runtime in minutes (0 if not running).
    /// </summary>
    public int CurrentCycleMinutes { get; set; }

    /// <summary>
    /// Whether furnace is currently running.
    /// </summary>
    public bool IsCurrentlyHeating { get; set; }

    /// <summary>
    /// Whether AC is currently running.
    /// </summary>
    public bool IsCurrentlyCooling { get; set; }

    /// <summary>
    /// Average outdoor temperature over last 24 hours.
    /// </summary>
    public double AvgOutdoorTemp24h { get; set; }

    /// <summary>
    /// Number of heating cycles in last 24 hours.
    /// </summary>
    public int HeatingCycles24h { get; set; }

    /// <summary>
    /// Projected heating runtime for tomorrow based on weather forecast.
    /// </summary>
    public int ProjectedHeatingMinutesTomorrow { get; set; }

    /// <summary>
    /// Tomorrow's forecasted average temperature.
    /// </summary>
    public double ForecastedAvgTempTomorrow { get; set; }

    /// <summary>
    /// Timestamp of last update.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    /// <summary>
    /// Formatted 24h runtime as hours:minutes.
    /// </summary>
    public string FormattedRuntime24h => $"{TotalHeatingMinutes24h / 60}h {TotalHeatingMinutes24h % 60}m";

    /// <summary>
    /// Formatted projected runtime as hours:minutes.
    /// </summary>
    public string FormattedProjectedRuntime => $"{ProjectedHeatingMinutesTomorrow / 60}h {ProjectedHeatingMinutesTomorrow % 60}m";

    /// <summary>
    /// Formatted current cycle time.
    /// </summary>
    public string FormattedCurrentCycle => CurrentCycleMinutes > 0
        ? $"{CurrentCycleMinutes}m"
        : "Not running";

    /// <summary>
    /// Creates mock runtime stats for demo mode.
    /// </summary>
    public static RuntimeStats CreateMockStats()
    {
        return new RuntimeStats
        {
            TotalHeatingMinutes24h = 342, // 5h 42m
            TotalCoolingMinutes24h = 0,
            CurrentCycleMinutes = 12,
            IsCurrentlyHeating = true,
            IsCurrentlyCooling = false,
            AvgOutdoorTemp24h = 14.2, // Cold day
            HeatingCycles24h = 18,
            ProjectedHeatingMinutesTomorrow = 380, // 6h 20m - colder tomorrow
            ForecastedAvgTempTomorrow = 8.5,
            LastUpdated = DateTime.Now
        };
    }
}
