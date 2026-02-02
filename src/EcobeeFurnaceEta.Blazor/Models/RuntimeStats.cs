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
    /// Whether furnace is currently running (any heat stage).
    /// </summary>
    public bool IsCurrentlyHeating { get; set; }

    /// <summary>
    /// Whether AC is currently running.
    /// </summary>
    public bool IsCurrentlyCooling { get; set; }

    /// <summary>
    /// Whether fan is running (without heat/cool).
    /// </summary>
    public bool IsFanRunning { get; set; }

    /// <summary>
    /// Whether primary/stage 1 heat is running.
    /// </summary>
    public bool IsPrimaryHeatRunning { get; set; }

    /// <summary>
    /// Whether auxiliary/secondary/stage 2+ heat is running.
    /// </summary>
    public bool IsAuxHeatRunning { get; set; }

    /// <summary>
    /// Whether heat pump is running.
    /// </summary>
    public bool IsHeatPumpRunning { get; set; }

    /// <summary>
    /// Raw equipment status string from Ecobee.
    /// </summary>
    public string EquipmentStatus { get; set; } = "";

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
    /// Average heat retention time in minutes (how long house stays warm after furnace off).
    /// </summary>
    public int AvgHeatRetentionMinutes { get; set; }

    /// <summary>
    /// Heat loss rate in °F per hour when furnace is off.
    /// </summary>
    public double HeatLossRatePerHour { get; set; }

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
    /// Formatted heat retention time.
    /// </summary>
    public string FormattedHeatRetention => AvgHeatRetentionMinutes > 0
        ? $"~{AvgHeatRetentionMinutes}m"
        : "Calculating...";

    /// <summary>
    /// Gets the current equipment status description.
    /// </summary>
    public string CurrentStatusDescription
    {
        get
        {
            if (IsAuxHeatRunning && IsPrimaryHeatRunning)
                return "Stage 1 + Aux Heat";
            if (IsAuxHeatRunning)
                return "Auxiliary Heat";
            if (IsHeatPumpRunning && IsPrimaryHeatRunning)
                return "Heat Pump + Stage 1";
            if (IsHeatPumpRunning)
                return "Heat Pump";
            if (IsPrimaryHeatRunning)
                return "Primary Heat";
            if (IsCurrentlyHeating)
                return "Heating";
            if (IsCurrentlyCooling)
                return "Cooling";
            if (IsFanRunning)
                return "Fan Only";
            return "Idle";
        }
    }

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
            IsFanRunning = true,
            IsPrimaryHeatRunning = true,
            IsAuxHeatRunning = false,
            IsHeatPumpRunning = false,
            EquipmentStatus = "fan,heat",
            AvgOutdoorTemp24h = 14.2, // Cold day
            HeatingCycles24h = 18,
            ProjectedHeatingMinutesTomorrow = 380, // 6h 20m - colder tomorrow
            ForecastedAvgTempTomorrow = 8.5,
            AvgHeatRetentionMinutes = 45, // House holds heat for ~45 min
            HeatLossRatePerHour = 1.2, // Loses ~1.2°F per hour
            LastUpdated = DateTime.Now
        };
    }
}
