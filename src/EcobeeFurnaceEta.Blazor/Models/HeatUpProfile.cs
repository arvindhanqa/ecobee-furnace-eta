namespace EcobeeFurnaceEta.Blazor.Models;

/// <summary>
/// Represents learned heat-up rates at different outdoor temperatures.
/// In Phase 1, this uses mock data. In Phase 4, it will be learned from runtime history.
/// </summary>
public class HeatUpProfile
{
    /// <summary>
    /// Heat-up rate data points: outdoor temp (F) -> heat-up rate (°F/min).
    /// </summary>
    public List<HeatUpDataPoint> DataPoints { get; set; } = new();

    /// <summary>
    /// Gets interpolated heat-up rate for a given outdoor temperature.
    /// </summary>
    public double GetHeatUpRate(double outdoorTempF)
    {
        if (DataPoints.Count == 0)
            return 0.28; // Default fallback

        // Find surrounding data points for interpolation
        var sorted = DataPoints.OrderBy(d => d.OutdoorTempF).ToList();

        // Below lowest data point
        if (outdoorTempF <= sorted[0].OutdoorTempF)
            return sorted[0].HeatUpRatePerMin;

        // Above highest data point
        if (outdoorTempF >= sorted[^1].OutdoorTempF)
            return sorted[^1].HeatUpRatePerMin;

        // Find the two surrounding points and interpolate
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            if (outdoorTempF >= sorted[i].OutdoorTempF && outdoorTempF <= sorted[i + 1].OutdoorTempF)
            {
                var lower = sorted[i];
                var upper = sorted[i + 1];
                var ratio = (outdoorTempF - lower.OutdoorTempF) / (upper.OutdoorTempF - lower.OutdoorTempF);
                return lower.HeatUpRatePerMin + ratio * (upper.HeatUpRatePerMin - lower.HeatUpRatePerMin);
            }
        }

        return 0.28; // Fallback
    }

    /// <summary>
    /// Creates a mock profile for a Saskatoon split-level home.
    /// Heat-up rate decreases as outdoor temp drops (harder to heat when colder outside).
    /// </summary>
    public static HeatUpProfile CreateMockSaskatoonProfile()
    {
        return new HeatUpProfile
        {
            DataPoints = new List<HeatUpDataPoint>
            {
                // Outdoor temp (F) -> Heat-up rate (°F/min)
                new() { OutdoorTempF = -22.0, HeatUpRatePerMin = 0.18 },  // -30°C - very cold
                new() { OutdoorTempF = -4.0, HeatUpRatePerMin = 0.22 },   // -20°C
                new() { OutdoorTempF = 14.0, HeatUpRatePerMin = 0.26 },   // -10°C
                new() { OutdoorTempF = 17.6, HeatUpRatePerMin = 0.28 },   // -8°C - current mock scenario
                new() { OutdoorTempF = 32.0, HeatUpRatePerMin = 0.32 },   // 0°C
                new() { OutdoorTempF = 50.0, HeatUpRatePerMin = 0.38 },   // 10°C
            }
        };
    }
}

/// <summary>
/// Single data point in heat-up profile.
/// </summary>
public class HeatUpDataPoint
{
    public double OutdoorTempF { get; set; }
    public double HeatUpRatePerMin { get; set; }
}
