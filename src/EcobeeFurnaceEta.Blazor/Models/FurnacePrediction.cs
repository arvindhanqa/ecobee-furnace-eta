namespace EcobeeFurnaceEta.Blazor.Models;

/// <summary>
/// Output of the prediction engine: ETA, status, and projections.
/// </summary>
public class FurnacePrediction
{
    /// <summary>
    /// Current furnace status.
    /// </summary>
    public FurnaceStatus Status { get; set; }

    /// <summary>
    /// Minutes until furnace will turn on (0 if already on or should be on now).
    /// </summary>
    public double MinutesToFurnaceOn { get; set; }

    /// <summary>
    /// Minutes until target temperature is reached.
    /// </summary>
    public double MinutesToTarget { get; set; }

    /// <summary>
    /// Heat loss rate in °F/min.
    /// </summary>
    public double HeatLossRatePerMin { get; set; }

    /// <summary>
    /// Gross heat-up rate (furnace output) in °F/min.
    /// </summary>
    public double HeatUpRatePerMin { get; set; }

    /// <summary>
    /// Effective (net) heat-up rate in °F/min (heat-up minus heat loss).
    /// </summary>
    public double EffectiveRatePerMin { get; set; }

    /// <summary>
    /// Projected temperatures at future time points.
    /// </summary>
    public List<TemperatureProjection> Projections { get; set; } = new();

    /// <summary>
    /// Temperature gap from current to setpoint.
    /// </summary>
    public double TempGapF { get; set; }

    /// <summary>
    /// Temperature at which furnace will kick on (setpoint - deadband).
    /// </summary>
    public double FurnaceKickOnTempF { get; set; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string StatusMessage => Status switch
    {
        FurnaceStatus.WillTurnOnNow => "WILL TURN ON NOW",
        FurnaceStatus.Running => "RUNNING",
        FurnaceStatus.WaitingForDeadband => $"Waiting (temp above kick-on threshold)",
        FurnaceStatus.AtTarget => "AT TARGET",
        FurnaceStatus.Cooling => "COOLING MODE",
        _ => "UNKNOWN"
    };

    /// <summary>
    /// Gets projected temperature at a given number of minutes from now.
    /// </summary>
    public double GetProjectedTemp(int minutesFromNow)
    {
        var projection = Projections.FirstOrDefault(p => p.MinutesFromNow == minutesFromNow);
        return projection?.ProjectedTempF ?? 0;
    }
}

/// <summary>
/// Temperature projection at a future time point.
/// </summary>
public class TemperatureProjection
{
    public int MinutesFromNow { get; set; }
    public double ProjectedTempF { get; set; }
    public bool AtOrAboveTarget { get; set; }
}

/// <summary>
/// Possible furnace states.
/// </summary>
public enum FurnaceStatus
{
    WillTurnOnNow,
    Running,
    WaitingForDeadband,
    AtTarget,
    Cooling
}
