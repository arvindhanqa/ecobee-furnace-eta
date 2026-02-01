namespace EcobeeFurnaceEta.Blazor.Models;

/// <summary>
/// Represents thermostat data from Ecobee API (or mock data in Phase 1).
/// </summary>
public class ThermostatData
{
    /// <summary>
    /// Current indoor temperature in Fahrenheit.
    /// </summary>
    public double CurrentTempF { get; set; }

    /// <summary>
    /// Target setpoint temperature in Fahrenheit.
    /// </summary>
    public double SetpointF { get; set; }

    /// <summary>
    /// Current outdoor temperature in Fahrenheit.
    /// </summary>
    public double OutdoorTempF { get; set; }

    /// <summary>
    /// Deadband/hysteresis in Fahrenheit. Furnace kicks on when temp drops below (setpoint - deadband).
    /// </summary>
    public double DeadbandF { get; set; } = 1.0;

    /// <summary>
    /// Whether the furnace is currently running.
    /// </summary>
    public bool FurnaceRunning { get; set; }

    /// <summary>
    /// Whether the system is currently heating.
    /// </summary>
    public bool IsHeating { get; set; }

    /// <summary>
    /// Whether the system is currently cooling.
    /// </summary>
    public bool IsCooling { get; set; }

    /// <summary>
    /// Name of the thermostat/zone.
    /// </summary>
    public string Name { get; set; } = "Main";

    /// <summary>
    /// Current HVAC mode (heat, cool, auto, off).
    /// </summary>
    public string HvacMode { get; set; } = "heat";

    /// <summary>
    /// Humidity percentage (0-100).
    /// </summary>
    public double HumidityPercent { get; set; }

    /// <summary>
    /// Timestamp of the data.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Creates mock data for Saskatoon winter scenario.
    /// </summary>
    public static ThermostatData CreateMockSaskatoonWinter()
    {
        return new ThermostatData
        {
            CurrentTempF = 68.0,
            SetpointF = 72.0,
            OutdoorTempF = 17.6, // -8°C converted to F
            DeadbandF = 1.0,
            FurnaceRunning = false,
            Name = "Saskatoon Split-Level",
            HvacMode = "heat",
            HumidityPercent = 35.0,
            Timestamp = DateTime.Now
        };
    }
}
