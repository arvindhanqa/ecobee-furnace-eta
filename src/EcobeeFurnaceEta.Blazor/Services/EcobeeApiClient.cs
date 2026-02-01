using EcobeeFurnaceEta.Blazor.Models;

namespace EcobeeFurnaceEta.Blazor.Services;

/// <summary>
/// Ecobee API client. Stub implementation for Phase 1-2 (returns mock data).
/// Phase 3 will implement actual OAuth and API calls.
/// </summary>
public class EcobeeApiClient
{
    /// <summary>
    /// Gets current thermostat data. Returns mock data in Phase 1.
    /// </summary>
    public Task<ThermostatData> GetThermostatDataAsync()
    {
        // Phase 1: Return mock Saskatoon winter data
        return Task.FromResult(ThermostatData.CreateMockSaskatoonWinter());
    }

    /// <summary>
    /// Gets heat-up profile. Returns mock profile in Phase 1.
    /// Phase 4 will build this from runtime history.
    /// </summary>
    public Task<HeatUpProfile> GetHeatUpProfileAsync()
    {
        // Phase 1: Return mock Saskatoon profile
        return Task.FromResult(HeatUpProfile.CreateMockSaskatoonProfile());
    }

    /// <summary>
    /// Gets today's schedule. Stub for Phase 1.
    /// </summary>
    public Task<List<ScheduleEvent>> GetTodayScheduleAsync()
    {
        // Mock schedule for a typical day
        var schedule = new List<ScheduleEvent>
        {
            new()
            {
                StartTime = new TimeOnly(6, 0),
                EndTime = new TimeOnly(8, 0),
                SetpointF = 72.0,
                Name = "Wake"
            },
            new()
            {
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(17, 0),
                SetpointF = 68.0,
                Name = "Away"
            },
            new()
            {
                StartTime = new TimeOnly(17, 0),
                EndTime = new TimeOnly(22, 0),
                SetpointF = 72.0,
                Name = "Home"
            },
            new()
            {
                StartTime = new TimeOnly(22, 0),
                EndTime = new TimeOnly(6, 0),
                SetpointF = 66.0,
                Name = "Sleep"
            }
        };

        return Task.FromResult(schedule);
    }
}

/// <summary>
/// A scheduled temperature event.
/// </summary>
public class ScheduleEvent
{
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public double SetpointF { get; set; }
    public string Name { get; set; } = "";
}
