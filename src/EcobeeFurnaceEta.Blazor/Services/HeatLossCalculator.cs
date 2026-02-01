namespace EcobeeFurnaceEta.Blazor.Services;

/// <summary>
/// Calculates heat loss rate based on indoor/outdoor temperature differential.
/// Uses simplified Newton's law of cooling.
/// </summary>
public class HeatLossCalculator
{
    /// <summary>
    /// Thermal constant for the home. Higher = more heat loss.
    /// This would be learned from actual data in Phase 4.
    /// For a Saskatoon split-level, using an estimated value.
    /// </summary>
    private const double ThermalConstant = 0.0012;

    /// <summary>
    /// Calculates heat loss rate in °F/min based on temperature differential.
    /// Heat loss is proportional to (indoor temp - outdoor temp).
    /// </summary>
    /// <param name="indoorTempF">Current indoor temperature in Fahrenheit</param>
    /// <param name="outdoorTempF">Current outdoor temperature in Fahrenheit</param>
    /// <returns>Heat loss rate in °F/min (always positive when indoor > outdoor)</returns>
    public double CalculateHeatLossRate(double indoorTempF, double outdoorTempF)
    {
        var tempDifferential = indoorTempF - outdoorTempF;

        // Heat loss only occurs when indoor is warmer than outdoor
        if (tempDifferential <= 0)
            return 0;

        // Heat loss rate = thermal constant × temperature differential
        // This gives us °F lost per minute
        return ThermalConstant * tempDifferential;
    }

    /// <summary>
    /// Projects temperature after given minutes with no heating.
    /// Uses exponential decay model (Newton's cooling law).
    /// </summary>
    public double ProjectTempWithNoHeating(double currentTempF, double outdoorTempF, int minutes)
    {
        var heatLossRate = CalculateHeatLossRate(currentTempF, outdoorTempF);
        // Simple linear approximation for short time periods
        // For more accuracy over longer periods, would use exponential decay
        return currentTempF - (heatLossRate * minutes);
    }

    /// <summary>
    /// Calculates how long until temperature drops to a threshold (no heating).
    /// </summary>
    public double MinutesUntilTemp(double currentTempF, double targetTempF, double outdoorTempF)
    {
        if (currentTempF <= targetTempF)
            return 0;

        var heatLossRate = CalculateHeatLossRate(currentTempF, outdoorTempF);
        if (heatLossRate <= 0)
            return double.MaxValue; // Won't reach target if no heat loss

        return (currentTempF - targetTempF) / heatLossRate;
    }
}
